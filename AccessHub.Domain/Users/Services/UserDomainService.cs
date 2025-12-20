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

        public async Task<User> CreateUserAsync(string username, string email, string password,string phoneNumber)
        {
            if (await _userRepository.ExistsAsync(username, email))
                throw new DomainException("User with same username or email already exists");

            var passwordHash = _passwordHasher.HashPassword(password);
            var user = new User(username, email, passwordHash,phoneNumber);

            await _userRepository.AddAsync(user);
            return user;
        }

        public async Task<User?>   ValidateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            return user.ValidatePassword(password) ? user : null;
        }
    }
}