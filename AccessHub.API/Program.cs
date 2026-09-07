using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using AccessHub.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using AccessHub.Domain;
using AccessHub.Infrastructure.Database;
using AccessHub.Infrastructure.OpenIddict;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Dev https：launchSettings 已声明 https://0.0.0.0:5700，此处为 https 端点提供自签证书
// （certs/dev.crt + dev.key，SAN 含 localhost / 127.0.0.1 / 192.168.52.129），
// 避免 Linux 上依赖 dotnet dev-certs。前端走 https 后，此处 https 可消除混合内容拦截。
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        var certPath = Path.Combine(builder.Environment.ContentRootPath, "certs", "dev.crt");
        var keyPath = Path.Combine(builder.Environment.ContentRootPath, "certs", "dev.key");
        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        }
    });
});

// Add services to the container.
builder.Services.AddAccessHubApp();
builder.Services.AddAccessHubDomain();
builder.Services.AddAccessHubInfra(builder.Configuration, builder.Environment);

builder.Services.AddOpenIddict()
    // 注册 OpenIddict 验证组件,保护本api资源
    .AddValidation(opt =>
                {
                    opt.AddAudiences("ahbapi");
                    opt.UseLocalServer();
                    //注册 ASP.NET Core 主机
                    //将验证系统挂到 ASP.NET Core Authentication 中,内部通过AddAuthentication().AddScheme()默认注入OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme。
                    //但不会默认成为全局 DefaultScheme， 需要手动指定services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    opt.UseAspNetCore();
                });
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    //options.LogoutPath = "/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    // 生产安全:cookie 仅 HTTPS 传输 + 防 JS 读取 + 同站 Lax(防 CSRF)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 数据保护密钥持久化:默认密钥存内存,后端重启 = 所有登录 cookie 失效。
// 持久化到文件后重启不踢人;多实例部署需共享同一目录(或 Redis)并 SetApplicationName 一致。
var keysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("AccessHub");

// 反向代理转发头:生产通常经 nginx 反代,需透传 X-Forwarded-Proto/For,
// 否则 UseHttpsRedirection 与 OpenIddict issuer 计算会基于内部 http 端口出错。
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // 生产应配置已知代理 IP,避免客户端伪造转发头:
    // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
});
// scope 与 permission 策略均由 AccessHubPolicyProvider 按命名约定动态解析,无需启动期 AddPolicy 注册。
//   ahb.<模块>     → 校验 token 的 scope claim(client 被授权访问的 API 模块)
//   perm.<资源>.<动作> → 校验 token 的 permission claim(user 被授权的动作,支持 *.* / 模块.all 通配)
// 新增 API 资源只需在 Controller 写 [Authorize(Policy="ahb.xxx"/"perm.xxx")],不再碰此处。
builder.Services.AddAuthorization();
// 动态策略提供器:覆盖默认 DefaultAuthorizationPolicyProvider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AccessHubPolicyProvider>();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-COOKIE";
    // SameSite=None 必须搭配 Secure,否则现代浏览器会拒绝该 cookie,
    // 导致 Razor Pages 表单 POST 时拿不到 antiforgery cookie → 校验失败返回 400。
    // SecurePolicy.SameAsRequest 在 HTTPS 下自动加 Secure 标志(应用为 HTTPS,故始终 Secure)。
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
// CORS 允许源从配置读取(appsettings.json Cors:AllowedOrigins),生产按需配置具体域名。
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 添加 Swagger 服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "ASP.NET Core Web API",
        Contact = new OpenApiContact { Name = "Dev Team", Email = "dev@company.com" }
    });

    // 使用 XML 注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddRazorPages();// Razor page. 登录页

// 限流:按 IP+路径分区,/connect/token 与 /Account/Login 防暴力枚举
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        // /connect/token:每分钟每 IP 10 次(防 token 暴力枚举)
        if (path == "/connect/token")
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"token:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }
        // /Account/Login:每分钟每 IP 5 次(防账号爆破)
        if (path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"login:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }
        // 其他端点不限流
        return RateLimitPartition.GetNoLimiter("default");
    });
});

// 健康检查:探活 + 数据库连通性(K8s/LB 用)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AccessHubDbContext>("database");

// 生产环境结构化日志(JSON 格式控制台,便于 ELK/Loki 采集);dev 保持可读文本
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await OpenIddictSetup.SeedAsync(sp);
}

// 配置 Swagger 中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        // Swagger 用 PKCE(Public 客户端不应有 client_secret);client_id 用前端实际 client
        c.OAuthClientId("userManageWebClient");
        c.OAuthUsePkce();
        //c.RoutePrefix = "api-docs"; // 自定义访问路径
    });
}

//在应用程序启动时调用数据库初始化方法
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AccessHubDbContext>();
        DbInitializer.InitializeDatabase(context); // 初始化数据库
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline.
// 转发头中间件置最前:让后续所有中间件(HTTPS 重定向/issuer 计算/限流)看到真实 proto/IP
app.UseForwardedHeaders();
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (errorFeature == null) return;

        if (errorFeature.Error is DomainException dex)
        {
            // 业务异常:预期内,记 Information,返回业务错误码
            logger.LogInformation("业务异常: {Message}", dex.Message);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { code = 1001, error = dex.Message });
            await context.Response.WriteAsync(result);
        }
        else
        {
            // 未预期异常:记 Error(含堆栈),返回通用错误(不泄露内部细节)
            logger.LogError(errorFeature.Error, "未处理的服务端异常: {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { code = 500, error = "服务器内部错误" });
            await context.Response.WriteAsync(result);
        }
    });
});

// HTTPS 重定向 + HSTS(仅非 dev;dev 自签证书不需要重定向)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// 安全响应头中间件(防止 MIME 嗅探、点击劫持等)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "0"; // 现代浏览器用 CSP;X-XSS-Protection 有副作用故禁用
    // TODO: 生产环境配置 Content-Security-Policy(SPA + 内联脚本需配 nonce 或 'unsafe-inline')
    await next();
});

app.UseRouting();
app.UseRateLimiter(); // 限流:认证前拦截暴力请求
app.UseCors();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapRazorPages();
// 健康检查端点(匿名,/health 返回含数据库检查的 JSON)
app.MapHealthChecks("/health");

app.Run();
