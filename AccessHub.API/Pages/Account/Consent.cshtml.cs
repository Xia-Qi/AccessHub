using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AccessHub.API.Pages.Account
{
    [IgnoreAntiforgeryToken]
    public class ConsentModel : PageModel
    {
        private readonly IOpenIddictApplicationManager _appManager;

        public ConsentModel(IOpenIddictApplicationManager appManager)
        {
            _appManager = appManager;
        }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }
        public string ApplicationName { get; set; }
        [BindProperty]
        public List<string> Scopes { get; set; }

        public async Task<ActionResult> OnGet(string returnUrl)
        {
            var uri = new Uri("http://localhost" + ReturnUrl);
            var query = QueryHelpers.ParseQuery(uri.Query);
            var clientId = query["client_id"];
            var scopes = query["scope"].ToString().Split(' ');
            var app = await _appManager.FindByClientIdAsync(clientId);
            var appName = await _appManager.GetDisplayNameAsync(app);
            Scopes = scopes.ToList();
            ApplicationName = appName;

            return Page();
        }

        public IActionResult OnPost(string action)
        {
            if (action == "no")
            {
                // 拒绝授权 -> 返回错误给客户端
                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User denied consent"
                });
                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // 同意授权 -> 继续回到 authorize 端点
            var p = new Dictionary<string, string>();
            p.Add("consent", "accepted");
            var redirctUrl = QueryHelpers.AddQueryString(ReturnUrl, p);
            return Redirect(redirctUrl);
                //                QueryHelpers.AddQueryString("/connect/authorize", Request.Query.Append(new KeyValuePair<string, StringValues>("consent", "accepted")));
        }
    }

}