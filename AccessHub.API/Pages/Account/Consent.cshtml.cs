using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.API.Pages.Account
{
    public class ConsentModel : PageModel
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        [BindProperty(SupportsGet = true)]
        public string ClientId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RedirectUri { get; set; }

        [BindProperty(SupportsGet = true)]
        public string State { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ResponseType { get; set; }

        public string ApplicationName { get; set; }
        public List<ScopeInfo> RequestedScopes { get; set; } = new();

        public ConsentModel(
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictScopeManager scopeManager)
        {
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (!result.Succeeded)
            {
                return Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = Request.Path + QueryString.Create(Request.Query)
                    },
                    IdentityConstants.ApplicationScheme);
            }

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            ApplicationName = await _applicationManager.GetDisplayNameAsync(application) ?? request.ClientId;

            var scopes = request.GetScopes();
            foreach (var scope in scopes)
            {
                var scopeInfo = await _scopeManager.FindByNameAsync(scope);
                if (scopeInfo != null)
                {
                    RequestedScopes.Add(new ScopeInfo
                    {
                        Name = scope,
                        Description = await _scopeManager.GetDescriptionAsync(scopeInfo) ?? scope
                    });
                }
                else
                {
                    RequestedScopes.Add(new ScopeInfo
                    {
                        Name = scope,
                        Description = scope
                    });
                }
            }

            ClientId = request.ClientId;
            Scope = string.Join(" ", scopes);
            RedirectUri = request.RedirectUri;
            State = request.State;
            ResponseType = request.ResponseType;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string button)
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (button == "deny")
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (!result.Succeeded)
            {
                return Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = Request.Path + QueryString.Create(Request.Query)
                    },
                    IdentityConstants.ApplicationScheme);
            }

            var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var userRoles = result.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            var clientId = await _applicationManager.GetClientIdAsync(application);

            var requestedScopes = request.GetScopes();
            var allowedScopes = (await _applicationManager.GetPermissionsAsync(application))
                .Where(p => p.StartsWith(Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Substring(Permissions.Prefixes.Scope.Length)).ToImmutableArray();
            var scopes = requestedScopes.Intersect(allowedScopes);

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);

            identity.AddClaim(OpenIddictConstants.Claims.Subject, userId);
            identity.AddClaim(OpenIddictConstants.Claims.Name, userName);
            identity.AddClaim(OpenIddictConstants.Claims.Email, userEmail);
            identity.AddClaim(OpenIddictConstants.Claims.ClientId, clientId);

            foreach (var role in userRoles)
            {
                identity.AddClaim(OpenIddictConstants.Claims.Role, role);
            }

            identity.SetDestinations(static claim => claim.Type switch
            {
                OpenIddictConstants.Claims.Subject => new[]
                {
                    Destinations.AccessToken,
                    Destinations.IdentityToken
                },
                OpenIddictConstants.Claims.Name => new[]
                {
                    Destinations.AccessToken,
                    Destinations.IdentityToken
                },
                OpenIddictConstants.Claims.Email => new[]
                {
                    Destinations.AccessToken,
                    Destinations.IdentityToken
                },
                OpenIddictConstants.Claims.Role => new[]
                {
                    Destinations.AccessToken,
                    Destinations.IdentityToken
                },
                OpenIddictConstants.Claims.ClientId => new[]
                {
                    Destinations.AccessToken
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
            principal.SetAudiences(resources);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        public class ScopeInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }
}