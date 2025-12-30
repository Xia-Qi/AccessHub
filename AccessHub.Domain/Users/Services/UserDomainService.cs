using AccessHub.Domain.Users.Model;

namespace AccessHub.Domain.Users.Services
{
    public class UserDomainService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserDomainService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?>   ValidateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            return _passwordHasher.VerifyPassword(password,user.PasswordHash) ? user : null;
        }
    }
}