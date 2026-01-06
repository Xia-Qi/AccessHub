using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users
{
    public interface IUserRepository : IRepository<User, UserId>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<bool> ExistsAsync(string username, string email);
        Task<(IEnumerable<User>, int)> GetUsersAsync(int page, int pageSize, string? search = null, bool? isActive = null);
        Task<(IEnumerable<Role>, int)> GetUserRolesAsync(UserId userId);
        Task AssignRolesToUserAsync(UserId userId, IEnumerable<RoleId> roleIds);
    }
}