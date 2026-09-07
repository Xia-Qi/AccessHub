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

        /// <summary>
        /// 聚合用户全部权限码(经 user→role→permission 链路,去重)。供 token 签发投放 permission claim。
        /// </summary>
        Task<IReadOnlyCollection<string>> GetPermissionsAsync(UserId userId);

        /// <summary>
        /// 聚合用户全部角色名(供 token 投放 role claim)。
        /// </summary>
        Task<IReadOnlyCollection<string>> GetRolesAsync(UserId userId);
    }
}