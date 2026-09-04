using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using AccessHub.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Repository
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AccessHubDbContext _dbContext;

        public RoleRepository(AccessHubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Role aggregate)
        {
            _dbContext.Roles.Add(aggregate);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Role aggregate)
        {
            _dbContext.Roles.Remove(aggregate);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string name, string code)
        {
            return await _dbContext.Roles
                .AnyAsync(r => r.Name == name || r.Code == code);
        }

        public async Task<Role> GetByCodeAsync(string code)
        {
            return await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Code == code);
        }

        public async Task<Role> GetByIdAsync(RoleId id)
        {
            return await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Role> GetByNameAsync(string name)
        {
            return await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<(IEnumerable<Role>, int)> GetRolesAsync(int page, int pageSize, string? search = null)
        {
            var query = _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsQueryable();

            // 应用搜索过滤
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search));
            }

            // 获取总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var roles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (roles, totalCount);
        }

        public async Task UpdateAsync(Role aggregate)
        {
            _dbContext.Roles.Update(aggregate);
            await Task.CompletedTask;
        }

        public async Task<(IEnumerable<Permission>, int)> GetRolePermissionsAsync(RoleId roleId)
        {
            var role = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return ([], 0);
            }

            var permissions = role.RolePermissions.Select(rp => rp.Permission);
            return (permissions, permissions.Count());
        }

        public async Task AssignPermissionsToRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds)
        {
            // 获取角色
            var role = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                throw new InvalidOperationException("角色不存在");
            }

            // 获取要分配的权限
            var permissions = await _dbContext.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            // 清除角色当前的所有权限
            role.RolePermissions.Clear();

            // 添加新的权限分配
            foreach (var permission in permissions)
            {
                role.RolePermissions.Add(new RolePermission(role, permission));
            }

            //await _dbContext.SaveChangesAsync();
        }
    }
}