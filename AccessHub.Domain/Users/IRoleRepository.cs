using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users
{
    public interface IRoleRepository : IRepository<Role, RoleId>
    {
        Task<Role> GetByNameAsync(string name);
        Task<Role> GetByCodeAsync(string code);
        Task<bool> ExistsAsync(string name, string code);
        Task<(IEnumerable<Role>, int)> GetRolesAsync(int page, int pageSize, string? search = null);
        Task<(IEnumerable<Permission>, int)> GetRolePermissionsAsync(RoleId roleId);
        Task AssignPermissionsToRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds);
    }
}