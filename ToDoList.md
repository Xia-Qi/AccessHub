# AccessHub DDD 优化 Todo 清单

最后更新：2026-08-24
状态图例：✅ 已完成  ·  ⬜ 待处理  ·  🚧 进行中  ·  ⛔ 阻塞
优先级：P0 运行时 Bug / 架构缺陷 → P1 设计规范 → P2 清理与一致性

---

## 已完成

| # | 项目 | 优先级 | 完成日 | 备注 |
|---|------|--------|--------|------|
| 01 | 修复 PhoneNumber 值对象（GetEqualityComponents 抛 NotImplementedException / IsValid 死代码 / IsPhoneNumber 恒 true） | P0 | 2026-08-24 | 构造即校验 + 正则校验中国手机号 |
| 03 | 仓储内部直接 `_dbContext.SaveChangesAsync()` — 双重 SaveChanges 风险 <br> `RoleRepository.cs:AssignPermissionsToRoleAsync` L132、`UserRepository.cs:AssignRolesToUserAsync` L198 仓储方法里自己 SaveChanges，与 Handler 中 `CommitTransactionAsync → SaveChangesAsync` 造成重复持久化调用，也破坏 UoW 事务语义 | P0 | 2026-08-31 | 去掉仓储方法内的 `_dbContext.SaveChangesAsync()`，只改内存态，持久化交回 Handler/UoW 统一提交 |

---

## 待处理

### P0 — 运行时 Bug / 架构缺陷

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 02 | 聚合边界被打破，三聚合根互相深层 Include | `UserRepository.cs` / `RoleRepository.cs` / `User.cs` / `Role.cs` / `Permission.cs` / `UserRole.cs` / `RolePermission.cs` | `User/Role/Permission` 均为 AggregateRoot，但通过导航集合互相引用；仓储 `GetByIdAsync` 做 `Include→ThenInclude→ThenInclude` 穿透三个聚合根 | (a) 明确唯一真正聚合根（建议以 `User` 为根，`Role`/`Permission` 改为普通实体或独立聚合但只通过 ID 引用）<br>(b) 去掉跨聚合导航，改为只保留 `RoleId`/`PermissionId` 引用<br>(c) 把"取用户角色+权限"拆成多次仓储查询或专门的读模型 Query | ⬜ |
| 04 | 聚合集合暴露为可变 `public ICollection<>`，外部绕过聚合直接改内部 | `User.cs:UserRoles`、`Role.cs:RolePermissions` / `UserRoles` | `public ICollection<UserRole> UserRoles { get; } = []` 外部可直接 Add/Clear，破坏聚合封装 | 改为 `IReadOnlyCollection`，新增 `AddRole(...)` / `RemoveRole(...)` / `ClearRoles()` 等聚合方法，所有集合变更通过领域方法完成 | ⬜ |

---

### P1 — 设计规范 / 死代码激活

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 05 | 领域事件机制"接了线没通电"，所有 AddDomainEvent 被注释，事件处理是空壳 | `User.cs`（构造 & SoftDelete 两处注释）、`UserCreatedDomainEvent.cs`、`UserCreatedDomainEventHandler.cs`、`UnitOfWork.cs:DispatchDomainEvents` | 已有的 UoW 分发逻辑正确，但实际从未产生任何事件 | 二选一：<br>(a) 激活：把 User 构造和 SoftDelete 的 AddDomainEvent 打开，完善 UserCreatedDomainEventHandler（如发送欢迎邮件、打审计日志）<br>(b) 删除：确认不需要领域事件，删除事件类/Handler/UnitOfWork.DispatchDomainEvents 降低心智负担 | ⬜ |
| 06 | `CreateUser` 未做唯一性校验（而 `CreateRole`/`CreatePermission` 已做） | `CreateUserCommandHandler.cs` / `IUserRepository.ExistsAsync` | `IUserRepository.ExistsAsync` 已定义但未在创建用户前调用，存在重名/重邮风险 | 创建前 `await _userRepository.ExistsAsync(request.Username, request.Email)` 校验，冲突抛 DomainException | ⬜ |
| 07 | Permission.UpdateXxx 没有任何校验，领域不变式缺失 | `Permission.cs:UpdateCode/UpdateName/UpdateDescription` | Role/User 的 Update 方法有非空校验，但 Permission 三方法直接赋值 | 同 Role 风格，对 `Code/Name` 至少做 `string.IsNullOrWhiteSpace` 校验并抛 DomainException，可选加长度上限 | ⬜ |
| 08 | AuthCommand 直接返回域实体 `User?`，领域模型穿透到表现层 | `AuthCommand.cs` / `AuthCommandHandler.cs` / `AuthController.cs` | 与查询层统一用 DTO 的做法矛盾，Controller 直接读 `user.Id/Name` | 新增 `AuthUserDto`（只含 Id/Name/Email 等需要的字段），Handler 返回 DTO 而非实体 | ⬜ |
| 09 | AggregateRoot.Version / IncrementVersion 死代码 + 未配并发令牌 | `AggregateRoot.cs` / `EntityBuilderExtensions.AggregateRootPropertyBuild` / 所有实体变更方法 | `IncrementVersion` 无人调用，Version 仅配了默认值 1 但不是并发令牌 | 二选一：<br>(a) 启用乐观锁：在每个变更方法（UpdateXxx/SoftDelete 等）末尾 `IncrementVersion()`，EF 里把 Version 配成 `[ConcurrencyToken]` 或 `IsRowVersion()`<br>(b) 暂时不用就删除 Version 字段与 IncrementVersion，避免误导 | ⬜ |
| 10 | 软删除两套机制并存，语义混乱 | `AccessHubDbContext.SaveChanges override`（L33-L44 把 EntityState.Deleted 转软删）、`User.SoftDelete()` 方法 | 一套是调用方 `user.SoftDelete()` 手动置位，一套是 `_dbContext.Remove(user)` 被拦截转软删；开发者很难判断哪种才是"正确用法" | 统一一种：<br>推荐保留 (b) `SoftDelete()` 领域方法，**删除** DbContext 的 SaveChanges 拦截转软删，避免将来误把 Remove 当真正删除用的人踩坑；或反过来明确规范并写文档 | ⬜ |
| 11 | 事务样板风格不一致（有的 try/catch rollback，有的没有） | `UpdateUserCommandHandler.cs`、`AssignUserRolesCommandHandler.cs`、`AssignRolePermissionsCommandHandler.cs` 与其它 Handler | CreateUser/DeleteUser/CreateRole 用 `Begin → try → Commit → catch Rollback`，但 UpdateUser、两个 Assign 没有 catch，抛异常时事务未回滚（虽然 DbContext 事务 Dispose 会隐式回滚，但语义不严谨） | 统一所有 Handler：要么全部 try/catch/rollback，要么把事务包裹抽成 Behavior/装饰器 | ⬜ |
| 12 | 异常类型混用：DomainException 与 InvalidOperationException | 所有 Application Handler / 仓储 | "not found / 不存在" 在不同地方用不同异常，上层无法统一分类处理 | 规则：<br>- 业务规则不满足 → `DomainException`<br>- 数据找不到 → 自定义 `EntityNotFoundException : DomainException` 或统一 `DomainException`<br>- 不合法程序调用 / 仓储未正确使用 → `InvalidOperationException`<br>AssignUserRoles 等 "用户/角色不存在" 应归为业务异常，抛 DomainException | ⬜ |

---

### P2 — 死代码清理 & 一致性 & 种子数据 & 编码

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 13 | `IRepository<T>.GetByIdAsync` 返回非空 `Task<TAggregate>`，但实现返回 null | `IRepository.cs` / 所有实现 | 签名误导，调用方不做 null 检查就会 NRE | 改成 `Task<TAggregate?> GetByIdAsync(TId id)`，并把所有 Handler 调用点的 null 判断保留或用 ! 标注 | ⬜ |
| 14 | 未使用的仓储方法 | `IUserRepository.GetUserRolesAsync / GetByEmailAsync`、`IRoleRepository.GetRolePermissionsAsync / GetByNameAsync / GetByCodeAsync`、`IPermissionRepository.GetByCodeAsync / GetByNameAsync / GetPermissionsByCodesAsync` | 定义并实现了但 Application 层无人调用（UpdatePermission 用了两个，其余都没） | 扫一遍确认真无人用的删除；暂时保留但计划用的在接口上 `[Obsolete("unused")]` 或 TODO 注释标记未来用途 | ⬜ |
| 15 | CreateUserCommandHandler 注入了 `UserDomainService` 但完全没用 | `CreateUserCommandHandler.cs` L12, L18, L23 | 构造注入了但 Handle 内从未访问，无意义依赖 | 删掉 `_userDomainService` 字段与构造参数 | ⬜ |
| 16 | 种子数据问题 | `DbInitializer.cs` L17-L20、L27-L29、L37-L40 | ① 用户参数顺序可能不对（当前 `new User("admin","123","123","111")`：userName=`"admin"`, email=`"123"`, passwordHash=`"123"`, phoneNumber=`"111"`），passwordHash 是明文、email 不是邮箱、phoneNumber 已因修复 #01 变成非法 | 改成真实示例数据：<br>`new User("admin", "admin@accesshub.com", _hashOf("Admin@123"), "13800000001")` 等；<br>**注意 Seed 是静态方法，没有 IPasswordHasher，需要把明文密码哈希后的字符串硬编码进来**（或者在 Program.cs 里走服务解析 IPasswordHasher 再 Seed） | ⬜ |
| 17 | 种子数据角色-权限、用户-角色关联未建立 | `DbInitializer.cs` L45-L76 全是注释 | 目前启动后 admin 用户没有 admin 角色，也就没有任何权限，API 上的 `[Authorize(Policy = "UserRead")]` 等都会 403 | 把注释的关联逻辑放开，正确通过 ValueObject Id 查找到对应实体后创建 UserRole / RolePermission | ⬜ |
| 18 | CQRS 文件命名/命名空间混乱 | `UpdateUserCommand.cs` / `DeleteUserCommand.cs` — 没有 namespace，Command 和 Handler 写在同一个文件里 | 与 `CreateUserCommand / Handler / Validator` 三文件拆分的风格不一致 | 统一：要么都拆分（Command / Handler / Validator 独立文件 + 正确 namespace `AccessHub.Application.Commands.Users`），要么都合并到一文件，至少命名空间一致 | ⬜ |
| 19 | 中文注释乱码（编码不一致） | `User.cs` L15-17、`Role.cs` L18、`DomainEventService.cs` L8-L9 等 | 文件保存为 GBK/ANSI，VSCode/IDE 按 UTF-8 打开显示为乱码 | 统一另存为 UTF-8（带 BOM 或不带 BOM 任选，但全项目一致），把乱码内容改为可读懂的中文 | ⬜ |
| 20 | DbInitializer.Seed 是同步方法，用 `context.SaveChanges()` 同步阻塞 | `DbInitializer.cs` | 在 ASP.NET Core 启动流程里同步 SaveChanges 属于不好的实践（容易线程池饥饿） | 改为 `SeedAsync` / `InitializeDatabaseAsync`，Program.cs 里 `await` 调用 | ⬜ |
| 21 | CreateUserCommandValidator 缺失手机号格式校验（配合 #01） | `CreateUserCommandValidator.cs` | 用户提交非法手机号时，错误会从领域层抛 DomainException，而不是应用层返回友好的 validation error | 加一条 `RuleFor(v => v.PhoneNumber).NotEmpty().Matches(@"^1[3-9]\d{9}$")`，与领域层正则保持一致 | ⬜ |
| 22 | Source 目录下的源码 / 测试项目未纳入审查（待你决定） | `/workspace/accesshub/AccessHub/Source`、`/workspace/accesshub/AccessHub/TokenTest`、`ClientSample` | 本清单只覆盖了 AccessHub.sln 主工程，附带目录未过 DDD 审查 | 主项目上述 1~21 完成后，单独跑一遍附带项目 | ⬜ |

---

## 建议执行顺序（批次化）

> 一次做一批，每批能独立编译通过，降低风险。

- **批次 1（必改 P0）**：#03（仓储双重 SaveChanges）+ #06（创建用户唯一性）+ #07（Permission 校验）+ #15（删除无用注入）+ #21（Validator 手机号）
- **批次 2（设计回归 P1）**：#04（聚合集合封装）+ #05（领域事件去留决策）+ #09（Version 并发令牌去留）+ #10（软删除统一策略）
- **批次 3（大重构 P0）**：#02（聚合边界）— 需要先决定 User/Role/Permission 三者关系，再动手
- **批次 4（一致性 P1）**：#08（Auth 改 DTO）+ #11（事务样板统一）+ #12（异常类型统一）+ #18（命名空间/文件风格）
- **批次 5（清理与种子 P2）**：#13 + #14 + #16 + #17 + #19 + #20

---

## 备注

- 任何改动后跑：`dotnet build` + `dotnet ef migrations script`（确认 EF 模型未被误改）。
- 涉及实体关系 (#02/#04/#17) 改动后需要评估是否需要新增 EF Migration。