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
