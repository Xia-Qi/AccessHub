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
                // 拒绝授权 -> 解析returnUrl，构造带有错误信息的重定向URL
                var uri = new Uri("http://localhost" + ReturnUrl);
                var query = QueryHelpers.ParseQuery(uri.Query);
                
                // 获取客户端的redirect_uri
                string? redirectUri = query.TryGetValue("redirect_uri", out var redirectUriValue) ? redirectUriValue.ToString() : null;
                
                if (!string.IsNullOrEmpty(redirectUri))
                {
                    // 构造错误参数
                    var errorParams = new Dictionary<string, string>{
                        { "error", OpenIddictConstants.Errors.AccessDenied },
                        { "error_description", "User denied consent" }
                    };
                    
                    // 如果有state参数，也需要带上
                    if (query.TryGetValue("state", out var stateValue))
                    {
                        errorParams.Add("state", stateValue.ToString());
                    }
                    
                    // 构造完整的重定向URL
                    var redirectUrl = QueryHelpers.AddQueryString(redirectUri, errorParams);
                    return Redirect(redirectUrl);
                }
                else
                {
                    // 如果没有redirect_uri，返回403错误
                    return StatusCode(403, "User denied consent");
                }
            }

            // 同意授权 -> 继续回到 authorize 端点
            var p = new Dictionary<string, string>();
            p.Add("consent", "accepted");
            var redirctUrl = QueryHelpers.AddQueryString(ReturnUrl, p);
            return Redirect(redirctUrl);
        }
    }

}