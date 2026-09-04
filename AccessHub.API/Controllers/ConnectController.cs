using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
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
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IPasswordHasher _passwordHasher;
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectController"/> class.
        /// </summary>
        /// <param name="users">The users<see cref="IUserRepository"/></param>
        public ConnectController(IUserRepository users, IOpenIddictApplicationManager applicationManager, IOpenIddictScopeManager scopeManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IPasswordHasher passwordHasher)
        {
            _users = users;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _authorizationManager = authorizationManager;
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
                    Scopes.OfflineAccess, // ✅ 添加 OfflineAccess 作用域，用于返回 refresh_token
                    "ahbapi.user.read",
                    "ahbapi.user.write",
                    "ahbapi.user.delete",
                    "ahbapi.client.read",
                    "ahbapi.client.write",
                    "ahbapi.client.delete"
                });

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            /* 4.刷新令牌模式
             * 当访问令牌过期时，使用刷新令牌获取新的访问令牌
             * POST /connect/token
                grant_type=refresh_token
                refresh_token=xxx
                client_id=xxx
                client_secret=yyy
             */
            if (request.IsRefreshTokenGrantType())
            {
                // 注意：刷新令牌会自动由OpenIddict进行验证：
                // 如果刷新令牌无效或已过期，此操作将不会被调用

                // 获取当前请求的身份验证结果
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // 获取与刷新令牌关联的用户标识符
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
                    Scopes.OfflineAccess, // ✅ 添加 OfflineAccess 作用域，用于支持 refresh_token
                    "ahbapi.user.read",
                    "ahbapi.user.write",
                    "ahbapi.user.delete",
                    "ahbapi.client.read",
                    "ahbapi.client.write",
                    "ahbapi.client.delete"
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
        /// 1. Authorization code 模式下，客户端发起登录，则首先进入该端点，
        /// a) 检查用户是否已登录
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/></returns>
        [HttpGet("/connect/authorize")]
        [HttpPost("/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            //获取openiddict的请求信息（client_id,scope等等）
            var request = HttpContext.GetOpenIddictServerRequest()!;
            //模拟cookie登录认证
            var cookieAuthResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            //判断是否登录成功（coockie）
            if (!cookieAuthResult.Succeeded)
            {
                // 用户未登录 → 显示登录页(cookie)
                var props = new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form : Request.Query)
                };

                return Challenge(props,
                    IdentityConstants.ApplicationScheme);
            }
            var userId = cookieAuthResult.Principal.Claims.First(a => a.Type == Claims.Subject).Value;
            var user = await _users.GetByIdAsync(new Domain.Users.Model.UserId(Guid.Parse(userId)));
            
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
            //判断是否已经永久授权,否则跳转到确认授权页面
            var authorizations = _authorizationManager.FindAsync(
                subject: userId,
                client: request.ClientId!,
                status: OpenIddictConstants.Statuses.Valid,
                type: OpenIddictConstants.AuthorizationTypes.Permanent,//永久授权
                scopes: request.GetScopes()
                );
            var permanented = false;
            await foreach(var auth in authorizations)
            {
                permanented = true;
                break;
            }
            if(!permanented && !"accepted".Equals(Request.Query["consent"]))
            {
                var consentUrl = Request.Path + Request.QueryString;
                return Redirect($"/Account/Consent?returnUrl={WebUtility.UrlEncode(consentUrl)}");
            }

            //生成授权码
            var identity = await CreateIdentity(request, user);
            return SignIn(
                new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var consentType = await _applicationManager.GetConsentTypeAsync(application);

            //Explicitc 每次都必须consent
            //Implicit 永不显示consent
            //External 自己控制
            //Systemmatic 默认
            if (consentType == OpenIddictConstants.ConsentTypes.Explicit
                || request.Prompt == "consent")
            {
                // 显示 Consent 页面
                var returnUrl = Request.Path + Request.QueryString;
                return Redirect($"/Account/Consent?returnUrl={WebUtility.UrlEncode(returnUrl)}");
            }
            
            

            // 用户已登录 → 继续授权
            //var principal = result.Principal!;
            // principal.SetScopes(request.GetScopes());
            // var identity = await CreateIdentity(request);
            // return SignIn(
            //     new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return Redirect($"/Account/Consent?{Request.QueryString.Value?.TrimStart('?')}");

            // 这里你可以实现自己的登录界面流程，示例直接返回 200
            //return Ok(new { message = "Authorization endpoint - implement UI/UX here." });
        }
        /// <summary>
        /// The Logout endpoint handles user logout requests.
        /// 前端调用示例（方式2：使用POST请求，包含id_token_hint）：
        /// fetch('/connect/logout', {
        ///   method: 'POST',
        ///   headers: {
        ///     'Content-Type': 'application/x-www-form-urlencoded'
        ///   },
        ///   body: new URLSearchParams({
        ///     id_token_hint: 'your-id-token',
        ///     post_logout_redirect_uri: window.location.origin
        ///   })
        /// });
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/></returns>
        [HttpPost("/connect/logout")]
        [HttpGet("/connect/logout")]
        public async Task<IActionResult> Logout()
        {
            // 获取OpenIddict注销请求
            var request = HttpContext.GetOpenIddictServerRequest();
            
            // 完整的注销流程：同时终止应用cookie和OpenIddict认证状态
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            // 增加OpenIddict认证方案的注销，确保完整清除OpenIddict的登录状态
            await HttpContext.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            // 处理令牌撤销 - 根据OpenIddict最佳实践，令牌撤销通过专门的/connect/revoke端点处理
            // 这里可以添加自定义的令牌撤销逻辑，例如记录日志或通知其他服务
            if (!string.IsNullOrEmpty(request?.IdTokenHint))
            {
                // 记录注销日志，包含id_token_hint的信息
                // 注意：不直接验证id_token，避免额外的计算开销和安全风险
            }
            
            // 处理注销后的重定向
            if (request != null && !string.IsNullOrEmpty(request.PostLogoutRedirectUri))
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
                if (application != null)
                {
                    var postLogoutRedirectUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application);
                    if (postLogoutRedirectUris.Contains(request.PostLogoutRedirectUri))
                    {
                        var redirectUrl = request.PostLogoutRedirectUri;
                        if (!string.IsNullOrEmpty(request.State))
                        {
                            redirectUrl += (redirectUrl.Contains('?') ? '&' : '?') + "state=" + WebUtility.UrlEncode(request.State);
                        }
                        return Redirect(redirectUrl);
                    }
                }
            }
            
            // 如果是GET请求，返回注销成功页面
            if (Request.Method == "GET")
            {
                return Content("<h2>您已成功注销</h2>", "text/html");
            }
            
            // POST请求返回成功响应
            return Ok(new { message = "注销成功" });
        }
        [HttpGet("/connect/userinfo")]
        public async Task<IActionResult> Userinfo()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var userId = result.Principal.FindFirst(Claims.Subject)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "invalid_token", error_description = "User ID not found" });
            }

            var user = await _users.GetByIdAsync(new UserId(Guid.Parse(userId)));
            if(user == null)
            {
                return NotFound(new { error = "user_not_found", error_description = "User not found" });
            }
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            roles.Add("admin");
            var userInfo = new
            {
                sub = user.Id.Value.ToString(),
                name = user.Name,
                email = user.Email,
                email_verified = true,
                roles = roles,
                scope = result.Principal.GetScopes()
            };

            return Ok(userInfo);
        }

        [HttpGet("/connect/device")]
        public IActionResult Device()
        {
            return Ok();
        }
        private async Task<ClaimsIdentity> CreateIdentity(OpenIddictRequest request,User? user)
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
            identity.AddClaim(Claims.Subject, user?.Id.Value.ToString() ?? clientId);
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
