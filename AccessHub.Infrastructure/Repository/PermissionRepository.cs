using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using AccessHub.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AccessHubDbContext _dbContext;

        public PermissionRepository(AccessHubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Permission aggregate)
        {
            _dbContext.Permissions.Add(aggregate);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Permission aggregate)
        {
            _dbContext.Permissions.Remove(aggregate);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string name, string code)
        {
            return await _dbContext.Permissions
                .AnyAsync(p => p.Name == name || p.Code == code);
        }

        public async Task<Permission> GetByCodeAsync(string code)
        {
            return await _dbContext.Permissions
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<Permission> GetByIdAsync(PermissionId id)
        {
            return await _dbContext.Permissions
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Permission> GetByNameAsync(string name)
        {
            return await _dbContext.Permissions
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByCodesAsync(IEnumerable<string> codes)
        {
            return await _dbContext.Permissions
                .Where(p => codes.Contains(p.Code))
                .ToListAsync();
        }

        public async Task UpdateAsync(Permission aggregate)
        {
            _dbContext.Permissions.Update(aggregate);
            await Task.CompletedTask;
        }

        public async Task<(IEnumerable<Permission>, int)> GetPermissionsAsync(int page, int pageSize, string? search = null)
        {
            var query = _dbContext.Permissions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search) || p.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var permissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (permissions, totalCount);
        }
    }
}