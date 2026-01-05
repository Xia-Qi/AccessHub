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
    private readonly IOpenIddictApplicationManager _appManager;

    public ConsentModel(IOpenIddictApplicationManager appManager)
    {
        _appManager = appManager;
    }

    [BindProperty]
    public string ReturnUrl { get; set; }
    public string ApplicationName { get; set; }
    public List<string> Scopes { get; set; }

    public async Task OnGet()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        if (request is null)
            throw new InvalidOperationException("Missing OpenIddict request.");

        ReturnUrl = Request.Query["returnUrl"];

        var clientId = request.ClientId;
        var app = await _appManager.FindByClientIdAsync(clientId);
        ApplicationName = await _appManager.GetDisplayNameAsync(app);

        // scopes requested by client
        Scopes = request.GetScopes().ToList();
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
        return Redirect(ReturnUrl!);
    }
}

}