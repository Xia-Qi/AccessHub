using System.Collections.Immutable;
using System.Security.Claims;
using AccessHub.Domain.Users;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Polly;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.API.Controllers
{
    /// <summary>
    /// Defines the <see cref="ConnectController" />
    /// </summary>
    [Route("connect")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        /// <summary>
        /// Defines the _users
        /// </summary>
        private readonly IUserRepository _users;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectController"/> class.
        /// </summary>
        /// <param name="users">The users<see cref="IUserRepository"/></param>
        public ConnectController(IUserRepository users, IOpenIddictApplicationManager applicationManager, IOpenIddictScopeManager scopeManager)
        {
            _users = users;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
        }

        /// <summary>
        /// The Exchange
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/></returns>
        [HttpPost("/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()!;

            /* 1.密码模式（弃用）
             * POST /connect/token
                grant_type=password
                username=alice
                password=123456
                client_id=xxx
                client_secret=yyy
             */
            if (request.IsPasswordGrantType())
            {
                var user = await _users.GetByUsernameAsync(request.Username!);
                if (user == null || !user.ValidatePassword(request.Password!))
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
                identity.AddClaim(OpenIddictConstants.Claims.Name, user.Name);
                identity.AddClaim(OpenIddictConstants.Claims.Email, user.Email);

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(new[] { OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles });

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            /* 2.客户端模式
             * 服务间访问，无用户，不需要浏览器
             * POST /connect/token
                Content-Type: application/x-www-form-urlencoded

                grant_type=client_credentials
                client_id=reporting-service
                client_secret=123456
                scope=ahbapi.user.read ahbapi.user.write

                +-------------+     client_id + client_secret       +----------------+
                |   Client    | ----------------------------------> | Authorization  |
                | (Server App)|                                      |    Server     |
                +-------------+                                      +----------------+
                        |                                                     |
                        |                   access_token                      |
                        <-----------------------------------------------------|
                        |
                        |   Authorization: Bearer access_token
                        V
                +----------------+
                |  Resource API  |
                +----------------+
             */
            if (request.IsClientCredentialsGrantType())
            {
                // 注意：客户端凭证会自动由OpenIddict进行验证：
                // 如果client_id或client_secret无效，此操作将不会被调用

                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                    throw new InvalidOperationException("The application cannot be found.");

                var clientId = await _applicationManager.GetClientIdAsync(application);

                // requestedScopes:客户端请求的 scopes; allowedScopes:客户端被允许的 scopes;
                var requestedScopes = request.GetScopes();
                var allowedScopes = (await _applicationManager.GetPermissionsAsync(application))
                    .Where(p => p.StartsWith(Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Substring(Permissions.Prefixes.Scope.Length)).ToImmutableArray();
                // 最终授权 scopes。客户端请求的 scope ∩ 客户端被允许的 scope ∩ 当前授权逻辑允许的 scope
                var scopes = requestedScopes.Intersect(allowedScopes);

                // 创建一个新的ClaimsIdentity，其中包含用于生
                // 成 id_token、token 或 code的声明.
                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);

                // 使用 client_id 作为主题标识符
                identity.AddClaim(Claims.Subject, clientId);
                identity.AddClaim(Claims.ClientId, clientId);
                //identity.AddClaim("tenant_id", tenantId);

                identity.SetDestinations(static claim => claim.Type switch
                {
                    "tenant_id" => new[]
                    {
                        Destinations.AccessToken
                    },

                    Claims.Subject => new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
                    },

                    _ => Array.Empty<string>()
                });

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(scopes);
                var resources = new List<string>();
                foreach (var scope in scopes)
                {   var scopeObj = await _scopeManager.FindByNameAsync(scope);
                    if (scopeObj == null)
                        continue;
                    var resource = await _scopeManager.GetResourcesAsync(scopeObj);
                    resources.AddRange(resource);
                }
                principal.SetAudiences(resources); //principal.SetResources("ahbapi");
                
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return BadRequest();
        }

        /// <summary>
        /// The Authorize
        /// 1. 浏览器访问 client 应用，client 跳转去 AccessHub（授权服务器）
        /// /connect/authorize?client_id=xxx&redirect_uri=callback
        /// 
        /// 3. AccessHub 返回 authorization code
        /// 302 → https://client/callback?code=xxx
        /// 
        /// 4. client 后端用 code 换 Token
        /// access_token
        /// id_token
        /// refresh_token
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/></returns>
        [HttpGet("/connect/authorize")]
        public async Task Authorize()
        {
            // 简化：若用户已登录（cookie），直接颁发 code；否则重定向到登录页面。
            var request = HttpContext.GetOpenIddictServerRequest()!;
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (!result.Succeeded)
            {
                // 用户未登录 → 显示登录页
                var props = new AuthenticationProperties
                {
                    RedirectUri = HttpContext.Request.Path + HttpContext.Request.QueryString
                };

                await HttpContext.ChallengeAsync(
                    IdentityConstants.ApplicationScheme, props); //跳转到登录页（Razor）,默认Account/Login.cshtml,可在openiddict配置中修改

                return;
            }

            // 用户已登录 → 继续授权
            var principal = result.Principal!;
            principal.SetScopes(request.GetScopes());

            await HttpContext.SignInAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);

            // 这里你可以实现自己的登录界面流程，示例直接返回 200
            //return Ok(new { message = "Authorization endpoint - implement UI/UX here." });
        }
        [HttpGet("/connect/device")]
        public IActionResult Device()
        {
            return Ok();
        }
    }
}
