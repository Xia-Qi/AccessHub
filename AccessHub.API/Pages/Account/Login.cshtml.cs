using AccessHub.Application.Queries.Users;
using AccessHub.Domain.Users.Model;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using OpenIddict.Abstractions;

namespace AccessHub.API.Pages.Account
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly IMediator _mediator;
        [BindProperty] 
        public InputModel Input { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }
        public LoginModel( IMediator mediator)
        {
            _mediator = mediator;
        }
        public void OnGet(string returnUrl)
        {
            ReturnUrl = returnUrl;
        }
        public async Task<IActionResult> OnPost()
        {

            var user = await _mediator.Send(new GetUserDetailsQuery(Input.Username));
            //if (user == null) return Page();
            // if(!user.ValidatePassword(Input.Password))
            // { return Page();
            // }
            //HttpContext.SignInAsync();
            //var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, false, false);iio
            //if (!result.Succeeded) return Page();
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "用户名或密码错误");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(OpenIddictConstants.Claims.Role, role));
            }

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

            return LocalRedirect(ReturnUrl);
        }
    }
    public class InputModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
