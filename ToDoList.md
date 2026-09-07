# AccessHub DDD 优化 Todo 清单

最后更新：2026-09-07
状态图例：✅ 已完成  ·  ⬜ 待处理  ·  🚧 进行中  ·  ⛔ 阻塞
优先级：P0 运行时 Bug / 架构缺陷 → P1 设计规范 → P2 清理与一致性

---

## 一、DDD 架构优化

### 已完成

| # | 项目 | 优先级 | 完成日 | 备注 |
|---|------|--------|--------|------|
| 01 | 修复 PhoneNumber 值对象（GetEqualityComponents 抛 NotImplementedException / IsValid 死代码 / IsPhoneNumber 恒 true） | P0 | 2026-08-24 | 构造即校验 + 正则校验中国手机号 |
| 03 | 仓储内部直接 `_dbContext.SaveChangesAsync()` — 双重 SaveChanges 风险（RoleRepository.AssignPermissionsToRoleAsync / UserRepository.AssignRolesToUserAsync） | P0 | 2026-08-31 | 去掉仓储方法内的 SaveChanges，持久化交回 Handler/UoW 统一提交 |
| 16 | 种子数据问题（明文密码 / 非法 email / 非法 phoneNumber） | P2 | 2026-09-04 | DbInitializer 重写：admin 用户用 DefaultPasswordHasher 哈希、email 规范、phoneNumber 规范；脏数据自愈机制 |
| 17 | 种子数据角色-权限、用户-角色关联未建立 | P2 | 2026-09-04 | admin 用户 → admin 角色 → *.* 权限（顶级通配，PolicyProvider 短路所有 permission 校验） |

---

### 待处理

#### P0 — 运行时 Bug / 架构缺陷

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 02 | 聚合边界被打破，三聚合根互相深层 Include | `UserRepository.cs` L69-99 / `Role.cs` / `Permission.cs` / `UserRole.cs` / `RolePermission.cs` | `User/Role/Permission` 均为 AggregateRoot，但仓储 `GetByIdAsync` 仍 `Include→ThenInclude→ThenInclude` 穿透三个聚合根 | (a) 明确唯一真正聚合根（建议以 User 为根，Role/Permission 改普通实体或只通过 ID 引用）<br>(b) 去掉跨聚合导航，改只保留 RoleId/PermissionId 引用<br>(c) "取用户角色+权限"拆成多次仓储查询或专门读模型 Query | ⬜ |
| 04 | 聚合集合暴露为可变 `public ICollection<>`，外部绕过聚合直接改内部 | `User.cs:UserRoles` L18、`Role.cs:RolePermissions/UserRoles` L17/19 | 仍为 `public ICollection<UserRole> UserRoles { get; } = []`，外部可直接 Add/Clear，破坏聚合封装 | 改为 `IReadOnlyCollection`，新增 `AddRole(...)` / `RemoveRole(...)` / `ClearRoles()` 聚合方法，集合变更通过领域方法 | ⬜ |

---

#### P1 — 设计规范 / 死代码激活

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 05 | 领域事件机制"接了线没通电"，AddDomainEvent 仍注释 | `User.cs` L40/L81（构造 & SoftDelete 两处注释）、`UserCreatedDomainEvent.cs`、`UserCreatedDomainEventHandler.cs`、`UnitOfWork.cs:DispatchDomainEvents` | UoW 分发逻辑正确，但实际从未产生任何事件 | 二选一：<br>(a) 激活：打开 AddDomainEvent，完善 Handler（欢迎邮件/审计日志）<br>(b) 删除：删除事件类/Handler/DispatchDomainEvents 降心智负担 | ⬜ |
| 06 | `CreateUser` 未做唯一性校验（CreateRole/CreatePermission 已做） | `CreateUserCommandHandler.cs` / `IUserRepository.ExistsAsync` | `ExistsAsync` 已定义但创建用户前未调用，存在重名/重邮风险 | 创建前 `await _userRepository.ExistsAsync(request.Username, request.Email)` 校验，冲突抛 DomainException | ⬜ |
| 07 | Permission.UpdateXxx 没有任何校验，领域不变式缺失 | `Permission.cs:UpdateCode/UpdateName/UpdateDescription` L26/31/36 | Role/User 的 Update 有非空校验，Permission 三方法直接赋值 | 同 Role 风格，对 Code/Name 做 `IsNullOrWhiteSpace` 校验并抛 DomainException，可选加长度上限 | ⬜ |
| 08 | AuthCommand 直接返回域实体 `User?`，领域模型穿透到表现层 | `AuthCommand.cs` / `AuthCommandHandler.cs` / `AuthController.cs` | Handler `return await _userDomainService.ValidateUserAsync(...)` 返回实体，Controller 直读 user.Id/Name | 新增 `AuthUserDto`（只含 Id/Name/Email 等必要字段），Handler 返回 DTO | ⬜ |
| 09 | AggregateRoot.Version / IncrementVersion 死代码 + 未配并发令牌 | `AggregateRoot.cs` L7/L9、所有实体变更方法 | `IncrementVersion` 无人调用，Version 仅默认值 1 且非并发令牌 | 二选一：<br>(a) 启用乐观锁：变更方法末尾 IncrementVersion()，EF 配 `[ConcurrencyToken]`/`IsRowVersion()`<br>(b) 删除 Version 字段与 IncrementVersion 避免误导 | ⬜ |
| 10 | 软删除两套机制并存，语义混乱 | `AccessHubDbContext.cs` L33-43（SaveChanges override 拦截 Deleted 转软删）、`User.SoftDelete()` | 一套手动 `SoftDelete()` 置位，一套 `_dbContext.Remove()` 被拦截转软删，开发者难判断正确用法 | 统一一种：推荐保留 (b) `SoftDelete()` 领域方法，**删除** DbContext 拦截转软删，避免误把 Remove 当真删踩坑；或反向明确规范并写文档 | ⬜ |
| 11 | 事务样板风格不一致（有的 try/catch rollback，有的没有） | `UpdateUserCommand.cs`、`AssignUserRolesCommand.cs`、`AssignRolePermissionsCommand.cs` vs `CreateUser/DeleteUser/CreateRole` | CreateUser/DeleteUser/CreateRole 有 `Begin→try→Commit→catch Rollback`，但 UpdateUser、两个 Assign 无 catch，抛异常时事务语义不严谨（Dispose 隐式回滚但语义不严谨） | 统一所有 Handler：要么全部 try/catch/rollback，要么抽成 Behavior/装饰器 | ⬜ |
| 12 | 异常类型混用：DomainException 与 InvalidOperationException | 所有 Application Handler / 仓储 | "not found / 不存在" 在不同地方用不同异常，上层无法统一分类处理 | 规则：业务规则不满足 → DomainException；数据找不到 → EntityNotFoundException:DomainException 或统一 DomainException；不合法程序调用 → InvalidOperationException。AssignUserRoles 等不存在应归 DomainException | ⬜ |

---

#### P2 — 死代码清理 & 一致性 & 编码

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| 13 | `IRepository<T>.GetByIdAsync` 返回非空 `Task<TAggregate>`，但实现返回 null | `IRepository.cs` L9 / 所有实现 | 签名 `Task<TAggregate> GetByIdAsync(TId id)` 误导，调用方不 null 检查会 NRE | 改成 `Task<TAggregate?> GetByIdAsync(TId id)`，Handler 调用点保留 null 判断或用 `!` 标注 | ⬜ |
| 14 | 未使用的仓储方法 | `IUserRepository.GetUserRolesAsync/GetByEmailAsync`、`IRoleRepository.GetRolePermissionsAsync/GetByNameAsync/GetByCodeAsync`、`IPermissionRepository.GetByCodeAsync/GetByNameAsync/GetPermissionsByCodesAsync` | 定义并实现但 Application 层无人调用 | 扫一遍确认真无人用的删除；保留但计划用的 `[Obsolete]` 或 TODO 标记 | ⬜ |
| 15 | CreateUserCommandHandler 注入 `UserDomainService` 但完全没用 | `CreateUserCommandHandler.cs` L12/L18/L23 | 构造注入了但 Handle 内从未访问 | 删掉 `_userDomainService` 字段与构造参数 | ⬜ |
| 18 | CQRS 文件命名/命名空间混乱 | `UpdateUserCommand.cs` / `DeleteUserCommand.cs` | 两文件**无 namespace 声明**，Command+Handler 同文件，与 CreateUser 拆分 + namespace 风格不一致 | 统一：要么都拆分（Command/Handler/Validator 独立文件 + namespace `AccessHub.Application.Commands.Users`），要么都合并，至少命名空间一致 | ⬜ |
| 19 | 中文注释乱码（编码不一致） | `User.cs` L15-17、`Role.cs`、`DomainEventService.cs` 等 | 文件保存为 GBK/ANSI，VSCode 按 UTF-8 打开显示乱码 | 统一另存为 UTF-8（带或不带 BOM 全项目一致），把乱码内容改为可读懂中文 | ⬜ |
| 20 | DbInitializer.Seed 是同步方法，用 `context.SaveChanges()` 同步阻塞 | `DbInitializer.cs` L19/L54/L65/L72/L81/L102/L122 | ASP.NET Core 启动流程里同步 SaveChanges 易线程池饥饿；`InitializeDatabase` 也同步 | 改为 `SeedAsync` / `InitializeDatabaseAsync`，Program.cs 里 `await` 调用 | ⬜ |
| 21 | CreateUserCommandValidator 缺失手机号格式校验（配合 #01） | `CreateUserCommandValidator.cs` | 当前仅 Username/Email/Password 规则，无 PhoneNumber；非法手机号会从领域层抛 DomainException 而非应用层友好 validation error | 加 `RuleFor(v => v.PhoneNumber).NotEmpty().Matches(@"^1[3-9]\d{9}$")`，与领域层正则一致 | ⬜ |
| 22 | Source 目录下的源码 / 测试项目未纳入审查 | `/workspace/accesshub/AccessHub/Source`、`TokenTest`、`ClientSample` | 本清单只覆盖 AccessHub.sln 主工程 | 主项目上述完成后，单独跑一遍附带项目 | ⬜ |

---

## 二、安全与部署优化

### 已完成

| # | 项目 | 完成日 | 备注 |
|---|------|--------|------|
| S01 | 数据保护密钥持久化 | 2026-09-05 | `PersistKeysToFileSystem` + `SetApplicationName("AccessHub")`，重启不踢人 |
| S02 | Identity cookie 安全标志 | 2026-09-05 | SecurePolicy=Always + HttpOnly + SameSite=Lax |
| S03 | 反向代理 ForwardedHeaders | 2026-09-05 | `UseForwardedHeaders` 透传 X-Forwarded-Proto/For，修正 issuer 与 https 重定向 |
| S04 | Authorize 端点校验用户状态 | 2026-09-05 | IsActive/IsDeleted 校验，失效身份清除 cookie 重新登录 |
| S05 | EF Core 版本统一 | 2026-09-05 | 全项目统一 9.0.3，消除降级警告 |
| S06 | 敏感日志清理 | 2026-09-05 | 全项目无明文 password/secret 日志 |
| S07 | CSRF 防护（antiforgery） | 2026-09-05 | antiforgery cookie Secure+SameSite=None + X-CSRF-TOKEN header，登录表单注入 token |
| S08 | 限流 | 2026-09-05 | /connect/token 10/min/IP、/Account/Login 5/min/IP 防暴力枚举 |
| S09 | 健康检查 + 结构化日志 | 2026-09-05 | /health（含 DbContext 检查）+ 生产 JSON 控制台日志 |
| S10 | scope/permission 三层分离重构 | 2026-09-07 | scope（模块级 ahb.*）+ permission（资源.动作 perm.*）+ role；动态 PolicyProvider，新增 API 不碰 Program.cs |
| S11 | CORS 配置化 | 2026-09-07 | origins 移到 appsettings.json `Cors:AllowedOrigins`，生产按需改配置 |
| S12 | 自签 https 证书 | 2026-09-04 | SAN 含 localhost/127.0.0.1/LAN IP，前后端共用，消除混合内容拦截 |
| S13 | OpenIddict 种子客户端迁移到运行时 API + JSON 种子 | 2026-09-04 | `seeds/*.json` upsert，配置演进无需改代码 |
| S14 | 种子 admin 用户 + *.* 权限 | 2026-09-04 | admin → admin 角色 → *.* 权限（顶级通配，PolicyProvider 短路） |
| S15 | 安全响应头 | 2026-09-05 | X-Content-Type-Options=nosniff / X-Frame-Options=DENY / Referrer-Policy / X-XSS-Protection=0 |
| S16 | Critical 漏洞修复（4 项） | 2026-09-05 | 登录密码校验 + admin 后门移除 + scope 越权（交集校验）+ CSRF |
| S17 | 密码哈希迭代升级 | 2026-09-07 | 100k → **600k**(OWASP 2023);版本化哈希格式 `pbkdf2_sha256$iter$salt$hash`;`ShouldRehash` 惰性升级旧哈希 |
| S18 | 账户锁定 Lockout | 2026-09-07 | User.AccessFailedCount + LockoutEnd;5 次失败锁定 15 分钟;登录成功重置计数 |

---

### 待处理

| # | 项目 | 影响文件 | 问题要点 | 修复建议 | 状态 |
|---|------|----------|----------|----------|------|
| S19 | 生产 OpenIddict 证书 | `AccessHubInfraServiceCollectionExtensions.cs` L100-102 | 用开发证书 + `DisableAccessTokenEncryption()`，生产 access token 不加密 | 生产换 X509 正式证书（`AddEncryptionCertificate`/`AddSigningCertificate`），删除 `DisableAccessTokenEncryption()` | ⬜ |
| S20 | CSP 安全头未实现 | `Program.cs` L265 TODO | 无 `Content-Security-Policy`，SPA 易受 XSS 注入 | 配置 CSP（SPA 内联脚本需 nonce 或 `'unsafe-inline'`，或用严格 CSP） | ⬜ |
| S21 | Logout id_token_hint 强制校验 | `ConnectController.cs` Logout | 仅依赖前端可选传 id_token_hint，公共客户端无强制校验 | 公共客户端建议强制要求 id_token_hint 或 state 校验，防 CSRF 注销 | ⬜ |
| S22 | HSTS 配置未细化 | `Program.cs` L258-261 | 仅 `UseHsts()` 默认 30 天，未配 `max-age`/`preload`/`includeSubDomains` | 生产配置 Hsts.MaxAge(1y).IncludeSubDomains().Preload() | ⬜ |
| S23 | ForwardedHeaders KnownProxies 未配 | `Program.cs` L78-79 | 注释提示生产需配，当前未配允许客户端伪造转发头 | 生产配置 `KnownProxies.Add(IPAddress.Parse(...))` 限定代理 IP | ⬜ |
| S24 | Refresh Token 滚动/重用检测 | `ConnectController.cs` 刷新逻辑 | 未显式做 token rotation/recycling，依赖 OpenIddict 默认行为 | 评估是否需要显式 token recycling（刷新时签发新 refresh，旧 refresh 失效） | ⬜ |
| S25 | UserInfo 端点未按 scope 过滤字段 | `ConnectController.cs` Userinfo | 当前全量返回 name/email/roles/scope，未按请求 scope 过滤 | 按 token scope 决定返回字段（如无 email scope 不返回 email） | ⬜ |

---

## 三、建议执行顺序（批次化）

> 一次做一批，每批能独立编译通过，降低风险。

- **批次 1（DDD 必改 P0）**：#06（创建用户唯一性）+ #07（Permission 校验）+ #15（删无用注入）+ #21（Validator 手机号）
- **批次 2（DDD 设计回归 P1）**：#04（聚合集合封装）+ #05（领域事件去留决策）+ #09（Version 并发令牌去留）+ #10（软删除统一策略）
- **批次 3（DDD 大重构 P0）**：#02（聚合边界）— 需先决定 User/Role/Permission 三者关系再动手
- **批次 4（DDD 一致性 P1）**：#08（Auth 改 DTO）+ #11（事务样板统一）+ #12（异常类型统一）+ #18（命名空间/文件风格）
- **批次 5（DDD 清理与编码 P2）**：#13 + #14 + #19 + #20
- **批次 6（安全基线 部署前必做）**：S19（生产证书）（S17 密码迭代 + S18 账户锁定 已完成）
- **批次 7（安全加固）**：S20（CSP）+ S21（Logout 强校验）+ S22（HSTS）+ S23（KnownProxies）
- **批次 8（可选）**：S24（Refresh Token 旋转）+ S25（UserInfo scope 过滤）

---

## 备注

- 任何改动后跑：`dotnet build AccessHub.API/AccessHub.API.csproj -c Debug`（确认 0 错误）+ `dotnet ef migrations script`（确认 EF 模型未被误改）。
- 涉及实体关系 (#02/#04/#17) 改动后需要评估是否需要新增 EF Migration。
- 安全项（S 系列）改动后建议用 curl 跑一遍 OAuth 全流程（authorize → token → userinfo → refresh）确认无回归。
- 重构后端重启即可生效的范围：种子（JSON/seeds）、CORS（appsettings.json `Cors:AllowedOrigins`）、issuer（appsettings.json `OpenIddict:Issuer`）。
