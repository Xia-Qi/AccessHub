using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Services;
using Domain.Base;
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
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public InputModel Input { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = string.Empty;

        public LoginModel(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public void OnGet(string returnUrl)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPost()
        {
            // 按用户名查用户;用户不存在或密码不匹配统一返回"用户名或密码错误",避免账号枚举。
            var user = await _users.GetByUsernameAsync(Input.Username);

            // 用户不存在:不做失败计数(无法关联到账户),统一错误信息。
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "用户名或密码错误");
                return Page();
            }

            // 锁定校验:锁定期间拒绝登录,不做密码校验(避免被用来探测)。
            if (user.IsLockedOut)
            {
                var remain = user.LockoutEnd!.Value - DateTime.UtcNow;
                ModelState.AddModelError(string.Empty, $"账号已被锁定,请 {(int)Math.Ceiling(remain.TotalMinutes)} 分钟后再试");
                return Page();
            }

            // 密码校验
            if (!_passwordHasher.VerifyPassword(Input.Password, user.PasswordHash))
            {
                // 失败计数 +1,达阈值锁定;持久化以确保跨请求累计。
                user.RecordFailedAccessAttempt();
                _users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "用户名或密码错误");
                return Page();
            }

            if (!user.IsActive || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "账号已被禁用");
                return Page();
            }

            // 登录成功:重置失败计数 + 惰性升级旧密码哈希
            user.ResetAccessFailedCount();
            if (_passwordHasher.ShouldRehash(user.PasswordHash))
            {
                user.UpdatePassword(_passwordHasher.HashPassword(Input.Password));
            }
            _users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

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
