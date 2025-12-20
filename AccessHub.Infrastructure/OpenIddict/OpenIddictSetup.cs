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

            if (await appManager.FindByClientIdAsync("app1") == null)
            {
                await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "app1",
                    ClientSecret = "app1-secret",
                    DisplayName = "Sample Client",
                    RedirectUris = { new Uri("https://app1.com/signin-oidc") },
                    Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "UserApiScope"
                },

                });
            }
            var scopeManager = sp.GetRequiredService<IOpenIddictScopeManager>();
            if (await scopeManager.FindByNameAsync("UserApiScope") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "UserApiScope",
                    Description = "UserApiScope",
                    Resources = { "UserApi" }
                }
                );
            }
        }
    }

}
