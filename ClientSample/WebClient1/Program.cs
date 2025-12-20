using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Client;
using OpenIddict.Client.AspNetCore;
using System.Net.Http;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// 在客户端应用中配置
builder.Services.AddOpenIddict()
    .AddClient(options =>
    {
        // 1. 配置与授权服务器的通信
        options.UseSystemNetHttp();  // 使用 HttpClient

        // 2. 配置客户端注册（替代 AddRegistration）
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://localhost:5700"),
            ClientId = "external-webapp",
            ClientSecret = "client-secret-from-accesshub",

            //// 响应类型
            //ResponseTypes =
            //{
            //    ResponseTypes.Code,
            //    ResponseTypes.IdToken,
            //    ResponseTypes.Token,
            //},

            //// 授权类型
            //GrantTypes =
            //{
            //    GrantTypes.AuthorizationCode,
            //    GrantTypes.ClientCredentials
            //},

            //// 重定向 URI
            //RedirectUris =
            //{
            //    new Uri("https://mywebapp.com/signin-oidc"),
            //    new Uri("https://localhost:5001/signin-oidc")
            //},

            //// 请求的 Scope
            //Scopes =
            //{
            //    Scopes.OpenId,
            //    Scopes.Profile,
            //    Scopes.Email,
            //    "api:read",
            //    "api:write"
            //},

            //// 其他配置
            //Requirements =
            //{
            //    Requirements.FrontChannelLogoutSupported,
            //    Requirements.Pkce
            //}
        });

        // 3. 配置令牌缓存（可选）
        //options.UseLocalCache();

        // 4. ASP.NET Core 集成
        options.UseAspNetCore(options =>
        {
            options.EnableRedirectionEndpointPassthrough();
            options.EnablePostLogoutRedirectionEndpointPassthrough();

            // 配置 Cookie 认证集成
            options.SetCookieAuthenticationOptions(new CookieAuthenticationOptions
            {
                LoginPath = "/login",
                LogoutPath = "/logout",
                AccessDeniedPath = "/access-denied",
                ExpireTimeSpan = TimeSpan.FromHours(1),
                SlidingExpiration = true,
                Cookie = new CookieBuilder
                {
                    Name = ".AccessHub.Auth",
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    SecurePolicy = CookieSecurePolicy.Always
                }
            });
        });
    });

// 5. 配置身份验证服务
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictClientAspNetCoreDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect(options =>
{
    // 自动绑定到 AddClient 配置
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
