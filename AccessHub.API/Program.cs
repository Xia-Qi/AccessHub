using System.Reflection;
using AccessHub.Infrastructure.Database;
using AccessHub.Infrastructure.OpenIddict;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAccessHubApp();
builder.Services.AddAccessHubDomain();
builder.Services.AddAccessHubInfra(builder.Configuration);

builder.Services.AddOpenIddict()
    // 注册 OpenIddict 验证组件,保护本api资源
    .AddValidation(opt =>
                {
                    opt.AddAudiences("UserApi");
                    // 从本地 OpenIddict 服务器实例导入配置,当授权服务和api在同一进程中时使用。
                    opt.UseLocalServer();
                    //注册 ASP.NET Core 主机
                    //将验证系统挂到 ASP.NET Core Authentication 中,内部通过AddAuthentication().AddScheme()默认注入OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme。
                    //但不会默认成为全局 DefaultScheme， 需要手动指定services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    opt.UseAspNetCore();
                });
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "UserApiScope");
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapRazorPages();

app.Run();
