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
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.delete"
                },

                });
            }
            if (await appManager.FindByClientIdAsync("userManageWebClient") == null)
            {
                await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "userManageWebClient",
                    ClientSecret = "userManageWebClient-secret",
                    DisplayName = "User Management Web Client (Pure Admin)",
                    RedirectUris = 
                    { 
                        new Uri("http://localhost:8848/callback"),
                        new Uri("http://localhost:8848/callback/"),
                        new Uri("http://127.0.0.1:8848/callback"),
                        new Uri("http://127.0.0.1:8848/callback/"),
                        new Uri("http://192.168.172.128:8848/callback"),
                        new Uri("http://192.168.172.128:8848/callback/")
                    },
                    PostLogoutRedirectUris = 
                    { 
                        new Uri("http://localhost:8848/login"),
                        new Uri("http://localhost:8848/login/"),
                        new Uri("http://127.0.0.1:8848/login"),
                        new Uri("http://127.0.0.1:8848/login/"),
                        new Uri("http://192.168.172.128:8848/login"),
                        new Uri("http://192.168.172.128:8848/login/")
                    },
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Token,
                        //Permissions.Endpoints.Logout,
                        Permissions.Endpoints.Revocation,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,
                        OpenIddictConstants.Permissions.Prefixes.Scope + "openid",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "profile",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "email",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "roles",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.read",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.write",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "ahbapi.user.delete",
                        OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
                        //Permissions.Prefixes.CodeChallenge + "S256"
                    },
                    // Requirements =
                    // {
                    //     Requirements.Features.ProofKeyForCodeExchange
                    // }
                });
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
        }
    }

}
