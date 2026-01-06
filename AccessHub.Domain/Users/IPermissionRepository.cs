using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users
{
    public interface IPermissionRepository : IRepository<Permission, PermissionId>
{
    Task<Permission> GetByCodeAsync(string code);
    Task<Permission> GetByNameAsync(string name);
    Task<bool> ExistsAsync(string name, string code);
    Task<IEnumerable<Permission>> GetPermissionsByCodesAsync(IEnumerable<string> codes);
    Task<(IEnumerable<Permission>, int)> GetPermissionsAsync(int page, int pageSize, string? search = null);
}
}