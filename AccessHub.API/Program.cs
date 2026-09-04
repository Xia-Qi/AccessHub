using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AccessHub.Domain;
using AccessHub.Infrastructure.Database;
using AccessHub.Infrastructure.OpenIddict;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddAccessHubInfra(builder.Configuration);

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
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        //policy.RequireClaim("scope", "ahbapi.user.read"); // 该写法有问题，token里多个scope时无法匹配
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.user.read"));
        });
    });
    options.AddPolicy("UserWrite", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.user.write"));
        });
    });
    options.AddPolicy("UserDelete", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.user.delete"));
        });
    });
    options.AddPolicy("ClientRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.client.read"));
        });
    });
    options.AddPolicy("ClientWrite", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.client.write"));
        });
    });
    options.AddPolicy("ClientDelete", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c =>
                c.Type == "scope" && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("ahbapi.client.delete"));
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-COOKIE";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});
builder.Services.AddCors(options =>
{
    //TODO: 生产环境请配置具体域名等信息，避免使用AllowAnyOrigin等宽松配置
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:8848",
            "http://127.0.0.1:8848",
            "http://192.168.52.129:8848",
            "https://localhost:8848",
            "https://127.0.0.1:8848",
            "https://192.168.52.129:8848"
        )
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
        c.OAuthClientId("userManageClient");
        c.OAuthClientSecret("userManageClient-secret");
        //c.OAuthUsePkce();
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
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (errorFeature != null && errorFeature.Error is DomainException dex)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            var result = System.Text.Json.JsonSerializer.Serialize(new { code = 1001, error = dex.Message });
            await context.Response.WriteAsync(result);
        }
    });
});

app.UseRouting();
app.UseCors();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapRazorPages();

app.Run();
