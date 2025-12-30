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
//        2���û��� AccessHub ��¼��Cookie��

//AccessHub ���� Cookie�������� AccessHub �Լ�����
        public async Task<IActionResult> OnPost()
        {
            // ������¼�߼�

            var user = await _mediator.Send(new GetUserDetailsQuery(Input.Username));
            if (user == null) return Page();
            // if(!user.ValidatePassword(Input.Password))
            // { return Page();
            // }
            //HttpContext.SignInAsync();
            //var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, false, false);iio
            //if (!result.Succeeded) return Page();

            // ��¼�ɹ���ص� authorize ԭʼ��ַ
            return LocalRedirect(ReturnUrl);
        }
    }
    public class InputModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
