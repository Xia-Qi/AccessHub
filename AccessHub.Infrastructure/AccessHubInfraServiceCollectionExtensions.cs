using AccessHub.Domain.Users;
using AccessHub.Infrastructure;
using AccessHub.Infrastructure.Database;
using AccessHub.Infrastructure.Repository;
using AccessHub.Infrastructure.Services;
using Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccessHubInfraServiceCollectionExtensions
    {
        public static IServiceCollection AddAccessHubInfra(this IServiceCollection services, IConfiguration configuration)
        {
            //Singleton 只能依赖 Singleton
            //Scoped 可以依赖 Scoped + Singleton
            //Transient 可以依赖任何生命周期
            services.TryAddScoped<IUserRepository, UserRepository>();//services.TryAddSingleton<IUserRepository, UserRepository>();
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
                    b => b.MigrationsAssembly(typeof(AccessHubDbContext).Assembly.FullName))
                .LogTo(Console.WriteLine, Logging.LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();

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

                    // 2. 启用授权端点
                    opt.SetTokenEndpointUris("/connect/token");
                    opt.SetAuthorizationEndpointUris("/connect/authorize");
                    opt.SetEndSessionEndpointUris("/connect/logout");
                    opt.SetUserInfoEndpointUris("/connect/userinfo");
                    opt.SetDeviceAuthorizationEndpointUris("/connect/device"); //设备码端点1/2
                    opt.SetEndUserVerificationEndpointUris("/connect/verify"); //设备码端点2/2
                    // 3. 启用支持的授权模式
                    opt.AllowAuthorizationCodeFlow();// 授权码流
                    opt.AllowAuthorizationCodeFlow();//.RequireProofKeyForCodeExchange();// 启用授权码模式 + PKCE
                    opt.AllowClientCredentialsFlow();// 客户端凭据流
                    opt.AllowRefreshTokenFlow();// 刷新令牌流
                    opt.AllowDeviceAuthorizationFlow();// 设备流
                    // 4. 令牌配置
                    opt//.RegisterScopes("ahbapi.user.read", "ahbapi.user.write","ahbapi.user.delete") // 注册作用域，作用域在令牌中表示访问权限。（实际项目应该根据permissions来注册作用域）
                           .SetAccessTokenLifetime(TimeSpan.FromHours(1));

                    // 5. 安全配置
                    //opt.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));
                           //.AddSigningCertificate(Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="));
                    // 注册签名和加密凭证。开发环境简化（切勿用于生产！）
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
                

            services.AddAuthentication();
            return services;
        }
    }
}