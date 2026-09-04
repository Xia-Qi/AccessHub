using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.Infrastructure.OpenIddict
{
    /// <summary>
    /// Defines the <see cref="OpenIddictSetup" />
    /// </summary>
    public static class OpenIddictSetup
    {
        /// <summary>
        /// The SeedAsync
        /// </summary>
        /// <param name="sp">The sp<see cref="IServiceProvider"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var appManager = sp.GetRequiredService<IOpenIddictApplicationManager>();

            if (await appManager.FindByClientIdAsync("userManageClient") == null)
            {
                await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "userManageClient",
                    ClientSecret = "userManageClient-secret",
                    DisplayName = "User Management Client",
                    RedirectUris = { new Uri("https://app1.com/signin-oidc") }, // TODO:
                    Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.write",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.delete",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.client.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.client.write",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.client.delete"
                },

                });
            }
            // userManageWebClient：纯 SPA 前端。
            // 采用「公共客户端 + PKCE」：不校验 client_secret（避免 secret 暴露在前端 bundle），
            // 改由 PKCE（code_challenge/code_verifier）防御授权码拦截。
            // 用 upsert 幂等写入，保证已有库记录也能随配置演进（重启即生效，无需手动删库）。
            {
                var webDescriptor = new OpenIddictApplicationDescriptor();
                ConfigureUserManageWebClient(webDescriptor);
                var existingWeb = await appManager.FindByClientIdAsync("userManageWebClient");
                if (existingWeb is null)
                {
                    // 新建：公共客户端，不设 ClientSecret。
                    await appManager.CreateAsync(webDescriptor);
                }
                else
                {
                    // 已存在：读出当前状态（含已哈希的 secret，public 客户端下不再校验、保留无害），
                    // 再用统一配置覆盖可变字段，最后写回。
                    // UpdateAsync(app, descriptor) 仅在 secret 变化时重新哈希，故原 secret 不会被破坏。
                    await appManager.PopulateAsync(webDescriptor, existingWeb);
                    ConfigureUserManageWebClient(webDescriptor);
                    await appManager.UpdateAsync(existingWeb, webDescriptor);
                }
            }
            var scopeManager = sp.GetRequiredService<IOpenIddictScopeManager>();
            if (await scopeManager.FindByNameAsync("ahbapi.user.read") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.user.read",
                    Description = "Get user information",
                    Resources = { "ahbapi" }
                }
                );
            }
            if (await scopeManager.FindByNameAsync("ahbapi.user.write") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.user.write",
                    Description = "Modify user information",
                    Resources = { "ahbapi" }
                }
                );
            }
            if (await scopeManager.FindByNameAsync("ahbapi.user.delete") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.user.delete",
                    Description = "Delete user",
                    Resources = { "ahbapi" }
                }
                );
            }
            if (await scopeManager.FindByNameAsync("ahbapi.client.read") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.client.read",
                    Description = "Get client information",
                    Resources = { "ahbapi" }
                }
                );
            }
            if (await scopeManager.FindByNameAsync("ahbapi.client.write") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.client.write",
                    Description = "Create/Update client and grant permissions to client",
                    Resources = { "ahbapi" }
                }
                );
            }
            if (await scopeManager.FindByNameAsync("ahbapi.client.delete") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "ahbapi.client.delete",
                    Description = "Delete client",
                    Resources = { "ahbapi" }
                }
                );
            }
            //default scopes
            var defaultScopes = new[] { Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess };
            foreach (var scope in defaultScopes)
            {
                if (await scopeManager.FindByNameAsync(scope) == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = scope,
                        Resources = { "AccessHub" }
                    });
                }
            }
        }

        /// <summary>
        /// 统一配置 userManageWebClient（公共客户端 + PKCE）。
        /// 供 SeedAsync 的 upsert 复用：create 与 update 走同一份配置，保证幂等。
        /// 注意：不设置 ClientSecret —— public 客户端不校验 secret，靠 PKCE 防御。
        /// </summary>
        private static void ConfigureUserManageWebClient(OpenIddictApplicationDescriptor descriptor)
        {
            descriptor.ClientId = "userManageWebClient";
            descriptor.DisplayName = "User Management Web Client (Pure Admin)";
            descriptor.ClientType = ClientTypes.Public; // 公共客户端：不校验 client_secret
            // 必须显式清空 secret：upsert 路径里 PopulateAsync 会把库里旧的 hashed secret
            // 拷进 descriptor，若不清，OpenIddict 校验「public 客户端不得带 secret」会拒绝 Update。
            descriptor.ClientSecret = null;

            // 回调地址同时注册 http 与 https 两套（前端 dev server 已启用自签 https，
            // redirect_uri 取页面 origin 动态生成，故两种协议、localhost/LAN IP 均需覆盖）
            descriptor.RedirectUris.Clear();
            descriptor.RedirectUris.Add(new Uri("http://localhost:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("http://localhost:8848/callback/"));
            descriptor.RedirectUris.Add(new Uri("http://127.0.0.1:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("http://127.0.0.1:8848/callback/"));
            descriptor.RedirectUris.Add(new Uri("http://192.168.52.129:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("http://192.168.52.129:8848/callback/"));
            descriptor.RedirectUris.Add(new Uri("https://localhost:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("https://localhost:8848/callback/"));
            descriptor.RedirectUris.Add(new Uri("https://127.0.0.1:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("https://127.0.0.1:8848/callback/"));
            descriptor.RedirectUris.Add(new Uri("https://192.168.52.129:8848/callback"));
            descriptor.RedirectUris.Add(new Uri("https://192.168.52.129:8848/callback/"));

            descriptor.PostLogoutRedirectUris.Clear();
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:8848/login/"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://127.0.0.1:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://127.0.0.1:8848/login/"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://192.168.52.129:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("http://192.168.52.129:8848/login/"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost:8848/login/"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://127.0.0.1:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://127.0.0.1:8848/login/"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://192.168.52.129:8848/login"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://192.168.52.129:8848/login/"));

            descriptor.Permissions.Clear();
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
            descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Roles);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.user.read");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.user.write");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.user.delete");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.client.read");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.client.write");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "ahbapi.client.delete");
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);

            // PKCE：OpenIddict 7.x 用 Requirement 启用（旧版 Permissions.Prefixes.CodeChallenge 已移除）。
            // 配合服务端 AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange() 双重强制。
            descriptor.Requirements.Clear();
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }
    }

}
