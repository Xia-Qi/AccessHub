using AccessHub.Application.Queries.Users;
using AccessHub.Domain.Users.Model;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccessHub.API.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IMediator _mediator;
        [BindProperty] 
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public LoginModel( IMediator mediator)
        {
            _mediator = mediator;
        }
        public void OnGet(string returnUrl)
        {
            ReturnUrl = returnUrl;
        }
//        2）用户在 AccessHub 登录（Cookie）

//AccessHub 产生 Cookie（仅用于 AccessHub 自己）。
        public async Task<IActionResult> OnPost()
        {
            // 处理登录逻辑

            var user = await _mediator.Send(new GetUserDetailsQuery(Input.Username));
            if (user == null) return Page();
            if(!user.ValidatePassword(Input.Password))
            { return Page();
            }
            //HttpContext.SignInAsync();
            //var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, false, false);iio
            //if (!result.Succeeded) return Page();

            // 登录成功后回到 authorize 原始地址
            return LocalRedirect(ReturnUrl);
        }
    }
    public class InputModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
