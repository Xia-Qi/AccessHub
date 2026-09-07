using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Services
{
    public class UserDomainService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public UserDomainService(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 校验用户名密码。校验通过后惰性升级旧密码哈希(S17),并重置失败计数。
        /// 返回 null 表示用户不存在或密码错误。
        /// </summary>
        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            // 锁定账户直接拒绝
            if (user.IsLockedOut) return null;

            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                // 失败计数 +1,达阈值锁定
                user.RecordFailedAccessAttempt();
                _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                return null;
            }

            // 成功:重置失败计数 + 惰性升级旧哈希
            user.ResetAccessFailedCount();
            if (_passwordHasher.ShouldRehash(user.PasswordHash))
            {
                user.UpdatePassword(_passwordHasher.HashPassword(password));
            }
            _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return user;
        }
    }
}