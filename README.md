# AccessHub V1

AccessHub是一个基于ASP.NET Core和OpenIddict构建的认证授权中心，为分散式系统提供统一的认证和授权。
<video src="./Source/Readme/SSO登录.mp4" controls width="500" height="300">
  您的浏览器不支持HTML5视频播放。
</video>

## 项目架构

### 分层架构

AccessHub采用经典的分层架构设计，确保代码的可维护性和可扩展性：

- **AccessHub.API**：API层，提供认证，授权和用户管理相关的接口
- **AccessHub.Application**：应用层，实现业务逻辑，处理业务规则和验证
- **AccessHub.Domain**：领域层，定义核心业务实体、值对象和领域服务
- **AccessHub.Infrastructure**：基础设施层，OpenIddict集成和数据库访问
- **Domain.Base**：领域基础层，提供通用的领域模型和接口

### 技术栈

| 技术/框架 | 版本 | 用途 |
|---------|------|------|
| ASP.NET Core | 8.0 | Web框架 |
| OpenIddict | 最新 | OAuth 2.0/OpenID Connect实现 |
| Entity Framework Core | 8.0 | ORM框架 |
| MySQL | 8.0 | 数据库 |
| Swagger/OpenAPI | 最新 | API文档 |
| ASP.NET Core Identity | 8.0 | 用户管理 |
| C# | 12.0 | 开发语言 |

## 核心功能

### 认证与授权

- ✅ 支持OAuth 2.0授权码流程
- ✅ 支持客户端凭证流程
- ✅ 基于角色的访问控制（RBAC）
- ✅ 基于范围（Scope）的权限控制

### 用户管理

- ✅ 用户注册和登录
- ✅ 角色分配和管理
- ✅ 密码重置和修改
- ✅ 用户信息获取

### 开发支持

- ✅ Swagger文档
- ✅ 数据库迁移
- ✅ 异常处理

## 快速开始

### 环境要求

- .NET 8.0 SDK
- MySQL 8.0
- Node.js 18.0
- Git

### 安装步骤

1. **克隆项目**

```bash
git clone https://github.com/your-repo/AccessHub.git
cd AccessHub
```

2. **创建数据库**

```bash
mysql -u root -p
CREATE DATABASE accesshub CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. **配置数据库连接**

编辑 `AccessHub.API/appsettings.json` 文件，修改数据库连接字符串：

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=accesshub;Uid=root;Pwd=your_password;"
}
```

4. **运行数据库迁移**

```bash
dotnet ef migrations add InitialCreate -p AccessHub.Infrastructure -s AccessHub.API
dotnet ef database update -p AccessHub.Infrastructure -s AccessHub.API
```

5. **启动应用**

后端(cd AccessHub)：
```bash
dotnet run --project AccessHub.API
```
管理页面(cd pure-admin-thin)：
```bash
pnpm dev
```

6. **访问应用**

- 登录页面：http://localhost:8848/login
- API地址：http://localhost:5700
- Swagger文档：http://localhost:5700/swagger/index.html

## 配置说明

### OpenIddict配置

OpenIddict的主要配置位于 `AccessHubInfraServiceCollectionExtensions.cs` 文件中：

```csharp
// 注册 OpenIddict
services.AddOpenIddict()
    .AddCore(opt =>
    {
        // 1. 使用EF Core存储
        opt.UseEntityFrameworkCore()
            .UseDbContext<AccessHubDbContext>();
    })
    .AddServer(opt =>
    {

        // 2. 启用授权端点
        opt.SetTokenEndpointUris("/connect/token");
    ......
```

### 认证配置

项目同时支持JWTBearer和Cookie认证：

```csharp
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});
```

### 权限策略

项目定义了基于范围的权限策略：

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.user.read"));
        });
    });
    // 更多策略...
});
```
## 持续更新

监控，日志，网关：可以参考该项目集成： https://github.com/Xia-Qi/APIGatewaySample
其它授权模式的实现
基础功能的实现：人员，权限，角色，权限范围的管理

## 许可证

AccessHub采用MIT许可证，详见LICENSE文件。

---
