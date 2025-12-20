using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users
{
    public interface IUserRepository : IRepository<User, UserId>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<bool> ExistsAsync(string username, string email);
    }
}