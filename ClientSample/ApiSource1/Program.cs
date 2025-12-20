using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenIddict()

    // 注册 OpenIddict 验证组件
    .AddValidation(options =>
    {
        // 从本地 OpenIddict 服务器实例导入配置
        options.UseLocalServer();

        // 注册 ASP.NET Core 主机
        options.UseAspNetCore();
    })
    .AddServer(options =>
    {
        // 注册加密凭证。此示例使用一个对称加密密钥，该密钥在服务器和API项目之间共享.
        //
        // 注意：在实际应用中，此加密密钥应存储在一个安全的地方
        // （例如，存储在Azure KeyVault中作为密钥）.
        //options.AddEncryptionKey(new SymmetricSecurityKey(
        //    Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        // 注册签名证书
        options.AddDevelopmentSigningCertificate();
    })
    .AddValidation(options =>
    {
        // 注意：验证处理器使用OpenID Connect发现机制来检索用于验证令牌的发行者签名密钥.
        options.SetIssuer("https://localhost:5700/");

        // 注册加密凭据。此示例使用对称加密密钥，该密钥在服务器与API项目之间共享
        //
        // 注意：在实际应用中，此加密密钥应当存储在一个安全的地方
        // （例如，在Azure KeyVault中，作为秘密存储）
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        // 注册 System.Net.Http 集成
        options.UseSystemNetHttp();

        // 注册ASP.NET Core主机
        options.UseAspNetCore();
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
