using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace AccessHub.API.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _passwordHasher;
        [BindProperty]
        public InputModel Input { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }
        public LoginModel(IUserRepository users, IPasswordHasher passwordHasher)
        {
            _users = users;
            _passwordHasher = passwordHasher;
        }
        public void OnGet(string returnUrl)
        {
            ReturnUrl = returnUrl;
        }
        public async Task<IActionResult> OnPost()
        {
            // 按用户名查用户;用户不存在或密码不匹配统一返回"用户名或密码错误",避免账号枚举。
            // 注意:此前实现把密码校验代码注释掉了,任意密码可登录任意账号(Critical 安全漏洞),现恢复。
            var user = await _users.GetByUsernameAsync(Input.Username);
            if (user == null || !_passwordHasher.VerifyPassword(Input.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "用户名或密码错误");
                return Page();
            }

            if (!user.IsActive || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "账号已被禁用");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(OpenIddictConstants.Claims.Subject, user.Id.Value.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in user.UserRoles.Select(ur => ur.Role.Name))
            {
                claims.Add(new Claim(OpenIddictConstants.Claims.Role, role));
            }

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            //使用 cookie 登录
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

            return Redirect(ReturnUrl);
        }
    }
    public class InputModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
