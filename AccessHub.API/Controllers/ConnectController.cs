using System.Collections.Immutable;
using System.Security.Claims;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
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
        private readonly IPasswordHasher _passwordHasher;
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectController"/> class.
        /// </summary>
        /// <param name="users">The users<see cref="IUserRepository"/></param>
        public ConnectController(IUserRepository users, IOpenIddictApplicationManager applicationManager, IOpenIddictScopeManager scopeManager,
            IPasswordHasher passwordHasher)
        {
            _users = users;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _passwordHasher = passwordHasher;
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
                if (user == null || !_passwordHasher.VerifyPassword(request.Password!, user.PasswordHash))
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
                {
                    var scopeObj = await _scopeManager.FindByNameAsync(scope);
                    if (scopeObj == null)
                        continue;
                    var resource = await _scopeManager.GetResourcesAsync(scopeObj);
                    resources.AddRange(resource);
                }
                principal.SetAudiences(resources); //principal.SetResources("ahbapi");

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            /* 3.授权码模式
             * 最常用，用于有用户参与的Web应用
             * 1. 客户端重定向到授权服务器获取授权码
             * 2. 用户登录并授权
             * 3. 授权服务器返回授权码
             * 4. 客户端使用授权码换取访问令牌
             * POST /connect/token
                grant_type=authorization_code
                code=xxx
                redirect_uri=callback
                client_id=xxx
                client_secret=yyy
             */
            if (request.IsAuthorizationCodeGrantType())
            {
                // 注意：授权码会自动由OpenIddict进行验证：
                // 如果授权码无效或已过期，此操作将不会被调用

                // 获取当前请求的身份验证结果
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // 获取与授权码关联的用户标识符
                var userId = result.Principal?.FindFirstValue(Claims.Subject);
                if (string.IsNullOrEmpty(userId))
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // 根据用户ID获取用户信息
                var user = await _users.GetByIdAsync(new Domain.Users.Model.UserId(Guid.Parse(userId)));
                if (user == null || user.IsDeleted || !user.IsActive)
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // 创建一个新的ClaimsIdentity
                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                identity.AddClaim(Claims.Subject, user.Id.Value.ToString());
                identity.AddClaim(Claims.Name, user.Name);
                identity.AddClaim(Claims.Email, user.Email);

                // 设置声明的目标
                identity.SetDestinations(static claim => claim.Type switch
                {
                    Claims.Name => new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
                    },
                    Claims.Email => new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
                    },
                    Claims.Subject => new[]
                    {
                        Destinations.AccessToken,
                        Destinations.IdentityToken
                    },
                    _ => Array.Empty<string>()
                });

                // 创建ClaimsPrincipal
                var principal = new ClaimsPrincipal(identity);
                
                // 设置允许的作用域
                principal.SetScopes(new[] 
                {
                    Scopes.OpenId, 
                    Scopes.Email, 
                    Scopes.Profile, 
                    Scopes.Roles,
                    "ahbapi.user.read",
                    "ahbapi.user.write",
                    "ahbapi.user.delete" 
                });

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
        [HttpPost("/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            var t = request.IsAuthorizationCodeGrantType();
            var t2 = request.GrantType;
            if (!result.Succeeded)
            {
                // 用户未登录 → 显示登录页
                var props = new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form : Request.Query)
                };

                return Challenge(props,
                    IdentityConstants.ApplicationScheme); //跳转到登录页（Razor）,默认Account/Login.cshtml,可在openiddict配置中修改

            }

            // 用户已登录 → 继续授权
            //var principal = result.Principal!;
            // principal.SetScopes(request.GetScopes());
            var identity = await CreateIdentity(request);
            return SignIn(
                new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // 这里你可以实现自己的登录界面流程，示例直接返回 200
            //return Ok(new { message = "Authorization endpoint - implement UI/UX here." });
        }
        [HttpPost("/connect/logout")]
        public IActionResult Logout()
        {
            return Ok();
        }
        [HttpGet("/connect/userinfo")]
        public IActionResult Userinfo()
        {
            return Ok();
        }

        [HttpGet("/connect/device")]
        public IActionResult Device()
        {
            return Ok();
        }
        private async Task<ClaimsIdentity> CreateIdentity(OpenIddictRequest request)
        {
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
            {
                var scopeObj = await _scopeManager.FindByNameAsync(scope);
                if (scopeObj == null)
                    continue;
                var resource = await _scopeManager.GetResourcesAsync(scopeObj);
                resources.AddRange(resource);
            }
            principal.SetAudiences(resources); //principal.SetResources("ahbapi");

            return identity;
        }
    }
}
