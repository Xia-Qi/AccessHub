using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.API.Controllers
{
    /// <summary>
    /// 客户端管理：增删改查 + 给客户端授权（scope / grant_type / endpoint / response_type / redirect_uri）。
    /// 直接复用 OpenIddict 的 <see cref="IOpenIddictApplicationManager"/>，无需新建实体或迁移。
    /// 受 ClientRead/ClientWrite/ClientDelete scope 策略保护。
    /// </summary>
    [Route("/api/client")]
    [ApiController]
    // 双层授权:Controller 级 scope(客户端被授权访问客户端管理模块)+ Action 级 permission
    [Authorize(Policy = "ahb.clientmgmt")]
    public class ClientController : ControllerBase
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public ClientController(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        /// <summary>客户端列表</summary>
        [HttpGet]
        [Authorize(Policy = "perm.client.read")]
        public async Task<IActionResult> Index()
        {
            var list = new List<object>();
            await foreach (var application in _applicationManager.ListAsync())
            {
                list.Add(await ToViewModel(application));
            }
            return Ok(list);
        }

        /// <summary>客户端详情</summary>
        [HttpGet("{clientId}")]
        [Authorize(Policy = "perm.client.read")]
        public async Task<IActionResult> Get(string clientId)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            return Ok(await ToViewModel(application));
        }

        /// <summary>创建客户端</summary>
        [HttpPost]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
                return BadRequest(new { error = "clientId is required" });
            if (string.IsNullOrWhiteSpace(request.ClientSecret))
                return BadRequest(new { error = "clientSecret is required" });

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = request.ClientId,
                ClientSecret = request.ClientSecret,
                DisplayName = request.DisplayName,
                ClientType = string.IsNullOrWhiteSpace(request.ClientType) ? "confidential" : request.ClientType,
                ApplicationType = string.IsNullOrWhiteSpace(request.ApplicationType) ? "web" : request.ApplicationType,
                ConsentType = request.ConsentType
            };
            AddUris(descriptor.RedirectUris, request.RedirectUris);
            AddUris(descriptor.PostLogoutRedirectUris, request.PostLogoutRedirectUris);
            AddRange(descriptor.Permissions, request.Permissions);

            var application = await _applicationManager.CreateAsync(descriptor);
            return Ok(new { id = await _applicationManager.GetIdAsync(application) });
        }

        /// <summary>
        /// 更新客户端基础信息（DisplayName / ClientType / ConsentType / RedirectUris / Permissions 等）。
        /// 注意：不会修改 client_secret，重置密钥请用 <see cref="ResetSecret"/>。
        /// </summary>
        [HttpPut("{clientId}")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> Update(string clientId, [FromBody] UpdateClientRequest request)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            // 读出现有值到 descriptor（包含已哈希的 secret），仅覆盖传入的字段，再写回。
            // UpdateAsync(app, descriptor) 仅在 secret 变化时重新哈希，这里不动 secret，故不会被破坏。
            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application);

            if (request.DisplayName is not null) descriptor.DisplayName = request.DisplayName;
            if (request.ClientType is not null) descriptor.ClientType = request.ClientType;
            if (request.ConsentType is not null) descriptor.ConsentType = request.ConsentType;
            if (request.ApplicationType is not null) descriptor.ApplicationType = request.ApplicationType;

            if (request.RedirectUris is not null)
            {
                descriptor.RedirectUris.Clear();
                AddUris(descriptor.RedirectUris, request.RedirectUris);
            }
            if (request.PostLogoutRedirectUris is not null)
            {
                descriptor.PostLogoutRedirectUris.Clear();
                AddUris(descriptor.PostLogoutRedirectUris, request.PostLogoutRedirectUris);
            }
            if (request.Permissions is not null)
            {
                descriptor.Permissions.Clear();
                AddRange(descriptor.Permissions, request.Permissions);
            }

            await _applicationManager.UpdateAsync(application, descriptor);
            return Ok();
        }

        /// <summary>删除客户端</summary>
        [HttpDelete("{clientId}")]
        [Authorize(Policy = "perm.client.delete")]
        public async Task<IActionResult> Delete(string clientId)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            await _applicationManager.DeleteAsync(application);
            return Ok();
        }

        /// <summary>查看客户端被授予的全部权限（scope / grant_type / endpoint / response_type）</summary>
        [HttpGet("{clientId}/permissions")]
        [Authorize(Policy = "perm.client.read")]
        public async Task<IActionResult> GetPermissions(string clientId)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            var permissions = await _applicationManager.GetPermissionsAsync(application);
            return Ok(GroupPermissions(permissions));
        }

        /// <summary>
        /// 给客户端授权 scope(如 ahb.usermgmt)。
        /// 请求体传 scope 名列表，将以全量替换方式重写该客户端所有 scope 权限。
        /// </summary>
        [HttpPut("{clientId}/scopes")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> GrantScopes(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplacePermissionsByPrefix(clientId, Permissions.Prefixes.Scope, request?.Items);

        /// <summary>给客户端授权 grant_type（authorization_code / refresh_token / client_credentials）</summary>
        [HttpPut("{clientId}/grant-types")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> GrantGrantTypes(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplacePermissionsByPrefix(clientId, Permissions.Prefixes.GrantType, request?.Items);

        /// <summary>给客户端授权可访问的端点（authorization / token / logout / revocation / userinfo）</summary>
        [HttpPut("{clientId}/endpoints")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> GrantEndpoints(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplacePermissionsByPrefix(clientId, Permissions.Prefixes.Endpoint, request?.Items);

        /// <summary>给客户端授权 response_type（code / token / id_token）</summary>
        [HttpPut("{clientId}/response-types")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> GrantResponseTypes(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplacePermissionsByPrefix(clientId, Permissions.Prefixes.ResponseType, request?.Items);

        /// <summary>设置客户端的 redirect_uri 列表</summary>
        [HttpPut("{clientId}/redirect-uris")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> SetRedirectUris(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplaceUris(clientId, redirectUris: request?.Items);

        /// <summary>设置客户端的 post_logout_redirect_uri 列表</summary>
        [HttpPut("{clientId}/post-logout-redirect-uris")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> SetPostLogoutRedirectUris(string clientId, [FromBody] GrantItemsRequest request)
            => await ReplaceUris(clientId, postLogoutRedirectUris: request?.Items);

        /// <summary>
        /// 重置客户端密钥。若请求体未提供 clientSecret，则自动生成一个 32 字节随机密钥。
        /// 返回明文密钥（仅此一次，入库前由 OpenIddict 自动哈希）。
        /// </summary>
        [HttpPost("{clientId}/secret")]
        [Authorize(Policy = "perm.client.write")]
        public async Task<IActionResult> ResetSecret(string clientId, [FromBody] ResetSecretRequest? request)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            var secret = string.IsNullOrWhiteSpace(request?.ClientSecret)
                ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
                : request.ClientSecret;

            // UpdateAsync(application, secret) 会自动哈希并持久化。
            await _applicationManager.UpdateAsync(application, secret);
            return Ok(new { clientId, clientSecret = secret });
        }

        // ---------------- 私有辅助 ----------------

        private async Task<IActionResult> ReplacePermissionsByPrefix(string clientId, string prefix, IEnumerable<string>? values)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application);

            // 仅移除指定前缀的权限，保留其它前缀权限不动。
            descriptor.Permissions.RemoveWhere(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (values is not null)
            {
                foreach (var v in values)
                {
                    var full = v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? v : prefix + v;
                    descriptor.Permissions.Add(full);
                }
            }

            await _applicationManager.UpdateAsync(application, descriptor);
            return Ok(await _applicationManager.GetPermissionsAsync(application));
        }

        private async Task<IActionResult> ReplaceUris(string clientId, IEnumerable<string>? redirectUris = null, IEnumerable<string>? postLogoutRedirectUris = null)
        {
            var application = await _applicationManager.FindByClientIdAsync(clientId);
            if (application is null)
                return NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application);

            if (redirectUris is not null)
            {
                descriptor.RedirectUris.Clear();
                AddUris(descriptor.RedirectUris, redirectUris);
            }
            if (postLogoutRedirectUris is not null)
            {
                descriptor.PostLogoutRedirectUris.Clear();
                AddUris(descriptor.PostLogoutRedirectUris, postLogoutRedirectUris);
            }

            await _applicationManager.UpdateAsync(application, descriptor);
            return Ok();
        }

        private async Task<object> ToViewModel(object application)
        {
            return new
            {
                id = await _applicationManager.GetIdAsync(application),
                clientId = await _applicationManager.GetClientIdAsync(application),
                displayName = await _applicationManager.GetDisplayNameAsync(application),
                clientType = await _applicationManager.GetClientTypeAsync(application),
                applicationType = await _applicationManager.GetApplicationTypeAsync(application),
                consentType = await _applicationManager.GetConsentTypeAsync(application),
                redirectUris = await _applicationManager.GetRedirectUrisAsync(application),
                postLogoutRedirectUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application),
                permissions = GroupPermissions(await _applicationManager.GetPermissionsAsync(application))
            };
        }

        private static object GroupPermissions(ImmutableArray<string> permissions)
        {
            var scopes = new List<string>();
            var grantTypes = new List<string>();
            var endpoints = new List<string>();
            var responseTypes = new List<string>();
            var others = new List<string>();

            foreach (var p in permissions)
            {
                if (p.StartsWith(Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase))
                    scopes.Add(p[Permissions.Prefixes.Scope.Length..]);
                else if (p.StartsWith(Permissions.Prefixes.GrantType, StringComparison.OrdinalIgnoreCase))
                    grantTypes.Add(p[Permissions.Prefixes.GrantType.Length..]);
                else if (p.StartsWith(Permissions.Prefixes.Endpoint, StringComparison.OrdinalIgnoreCase))
                    endpoints.Add(p[Permissions.Prefixes.Endpoint.Length..]);
                else if (p.StartsWith(Permissions.Prefixes.ResponseType, StringComparison.OrdinalIgnoreCase))
                    responseTypes.Add(p[Permissions.Prefixes.ResponseType.Length..]);
                else
                    others.Add(p);
            }

            return new { scopes, grantTypes, endpoints, responseTypes, others };
        }

        private static void AddUris(ICollection<Uri> target, IEnumerable<string>? uris)
        {
            if (uris is null) return;
            foreach (var u in uris)
            {
                if (Uri.TryCreate(u, UriKind.Absolute, out var uri))
                    target.Add(uri);
            }
        }

        private static void AddRange(HashSet<string> target, IEnumerable<string>? values)
        {
            if (values is null) return;
            foreach (var v in values)
                target.Add(v);
        }

        // ---------------- 请求 DTO ----------------

        public class CreateClientRequest
        {
            public string? ClientId { get; set; }
            public string? ClientSecret { get; set; }
            public string? DisplayName { get; set; }
            public string? ClientType { get; set; }       // confidential | public
            public string? ApplicationType { get; set; }  // web | native
            public string? ConsentType { get; set; }     // explicit | implicit | external | systematic
            public List<string>? RedirectUris { get; set; }
            public List<string>? PostLogoutRedirectUris { get; set; }
            public List<string>? Permissions { get; set; } // 完整权限串,如 scp:ahb.usermgmt 或直接 ahb.usermgmt
        }

        public class UpdateClientRequest
        {
            public string? DisplayName { get; set; }
            public string? ClientType { get; set; }
            public string? ApplicationType { get; set; }
            public string? ConsentType { get; set; }
            public List<string>? RedirectUris { get; set; }
            public List<string>? PostLogoutRedirectUris { get; set; }
            public List<string>? Permissions { get; set; }
        }

        public class GrantItemsRequest
        {
            public List<string>? Items { get; set; }
        }

        public class ResetSecretRequest
        {
            public string? ClientSecret { get; set; }
        }
    }
}
