using AccessHub.Domain.Users;
using AccessHub.Infrastructure;
using AccessHub.Infrastructure.Database;
using AccessHub.Infrastructure.Repository;
using AccessHub.Infrastructure.Services;
using Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccessHubInfraServiceCollectionExtensions
    {
        public static IServiceCollection AddAccessHubInfra(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Singleton 只能依赖 Singleton
            //Scoped 可以依赖 Scoped + Singleton
            //Transient 可以依赖任何生命周期
            services.TryAddScoped<IUserRepository, UserRepository>();//services.TryAddSingleton<IUserRepository, UserRepository>();
            services.TryAddScoped<IRoleRepository, RoleRepository>();
            services.TryAddScoped<IPermissionRepository, PermissionRepository>();
            services.TryAddScoped<ICurrentUserService, CurrentUserService>();
            services.TryAddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<AuditableEntitySaveChangesInterceptor>();

            services.AddDbContext<AccessHubDbContext>((sp,options) =>
            {
                //options.UseSqlServer(
                //    configuration.GetConnectionString("DefaultConnection"),
                //    b => b.MigrationsAssembly(typeof(AccessHubDbContext).Assembly.FullName));
                options.UseMySql(
                    configuration.GetConnectionString("Mysql"),
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    b => b.MigrationsAssembly(typeof(AccessHubDbContext).Assembly.FullName));

                // 敏感日志会打印 SQL 含参数(密码等),仅开发环境启用;生产环境关闭以防泄露。
                if (env.IsDevelopment())
                {
                    options.LogTo(Console.WriteLine, Logging.LogLevel.Information)
                           .EnableSensitiveDataLogging()
                           .EnableDetailedErrors();
                }

                // 配置 OpenIddict 使用的存储上下文
                options.UseOpenIddict();
            });

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

                    // 显式指定 Issuer：dev 环境下 Vite 代理会把 Host 改写为 localhost:5700，
                    // 而 OAuth 流实际从 192.168.52.129:5700 签发 token，导致 iss 与验证期望不一致 → API 401。
                    // 设置固定 Issuer 后，签发与验证共用同一值，不再依赖请求 Host 计算。
                    // 生产环境应改为对外可达的固定 URL（见 appsettings.json → OpenIddict:Issuer）。
                    var issuer = configuration["OpenIddict:Issuer"];
                    if (!string.IsNullOrWhiteSpace(issuer))
                    {
                        opt.SetIssuer(issuer);
                    }

                    // 2. 启用授权端点
                    opt.SetTokenEndpointUris("/connect/token");
                    opt.SetAuthorizationEndpointUris("/connect/authorize");
                    opt.SetEndSessionEndpointUris("/connect/logout");
                    opt.SetUserInfoEndpointUris("/connect/userinfo");
                    opt.SetDeviceAuthorizationEndpointUris("/connect/device"); //设备码端点1/2
                    opt.SetEndUserVerificationEndpointUris("/connect/verify"); //设备码端点2/2
                    // 3. 启用支持的授权模式
                    // 授权码流 + 强制 PKCE（对所有走授权码流程的客户端生效）。
                    // userManageClient 仅用 client_credentials，不受影响。
                    opt.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                    opt.AllowClientCredentialsFlow();// 客户端凭据流
                    opt.AllowRefreshTokenFlow();// 刷新令牌流
                    opt.AllowDeviceAuthorizationFlow();// 设备流
                    // 4. 令牌配置
                    opt//.RegisterScopes("ahb.usermgmt", "ahb.clientmgmt") // scope 由 EnsureScopesAsync 动态注册到 scope manager,此处不硬编码
                           .SetAccessTokenLifetime(TimeSpan.FromMinutes(3))
                           .SetRefreshTokenLifetime(TimeSpan.FromDays(1)); // 刷新令牌过期时间

                    // 5. 安全配置
                    // ⚠️ 开发环境用 OpenIddict 自动生成的临时开发证书;生产环境必须替换为正式 X509 证书,
                    // 否则 OpenIddict 启动时会拒绝。生产配置示例(取消注释并替换为真实证书路径/密码):
                    //   var encCert = new X509Certificate2("path/to/enc.pfx", "password");
                    //   var sigCert = new X509Certificate2("path/to/sig.pfx", "password");
                    //   opt.AddEncryptionCertificate(encCert).AddSigningCertificate(sigCert);
                    //   并删除下方 DisableAccessTokenEncryption()(生产应加密 access token)。
                    opt.AddDevelopmentEncryptionCertificate();
                    opt.AddDevelopmentSigningCertificate();
                    opt.DisableAccessTokenEncryption();//不加密令牌，仅用于开发环境

                    // 6. ASP.NET Core集成（注册 ASP.NET Core 主机并且配置 ASP.NET Core 选项）
                    opt.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()//允许你自己定义控制器/逻辑来处理授权 UI 或 token 响应，而不是 OpenIddict 内建页面
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();

                    // 其它：安全防护相关配置
                });
                

            return services;
        }
    }
}