using AccessHub.Domain.Users;
using AccessHub.Domain.Users.Model;
using AccessHub.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Repository
{
    /// <summary>
    /// Defines the <see cref="UserRepository" />
    /// </summary>
    public class UserRepository : IUserRepository
    {
        /// <summary>
        /// Defines the _dbContext
        /// </summary>
        private readonly AccessHubDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="dbContext">The dbContext<see cref="AccessHubDbContext"/></param>
        public UserRepository(AccessHubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// The AddAsync
        /// </summary>
        /// <param name="aggregate">The aggregate<see cref="User"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task AddAsync(User aggregate)
        {
            _dbContext.Users.Add(aggregate);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The DeleteAsync
        /// </summary>
        /// <param name="aggregate">The aggregate<see cref="User"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task DeleteAsync(User aggregate)
        {
            _dbContext.Users.Remove(aggregate);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The ExistsAsync
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <param name="email">The email<see cref="string"/></param>
        /// <returns>The <see cref="Task{bool}"/></returns>
        public async Task<bool> ExistsAsync(string username, string email)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Name == username || u.Email == email);
        }

        /// <summary>
        /// The GetByEmailAsync
        /// </summary>
        /// <param name="email">The email<see cref="string"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public async Task<User> GetByEmailAsync(string email)
        {
            return await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        /// <summary>
        /// The GetByIdAsync
        /// </summary>
        /// <param name="id">The id<see cref="UserId"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public async Task<User> GetByIdAsync(UserId id)
        {
            return await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// The GetByUsernameAsync
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(x => x.Name == username);
        }

        /// <summary>
        /// The UpdateAsync
        /// </summary>
        /// <param name="aggregate">The aggregate<see cref="User"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task UpdateAsync(User aggregate)
        {
            _dbContext.Users.Update(aggregate);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The GetUsersAsync
        /// </summary>
        /// <param name="page">The page<see cref="int"/></param>
        /// <param name="pageSize">The pageSize<see cref="int"/></param>
        /// <param name="search">The search<see cref="string"/></param>
        /// <param name="isActive">The isActive<see cref="bool?"/></param>
        /// <returns>The <see cref="Task{(IEnumerable{User}, int)}"/></returns>
        public async Task<(IEnumerable<User>, int)> GetUsersAsync(int page, int pageSize, string? search = null, bool? isActive = null)
        {
            var query = _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // 应用搜索过滤
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }

            // 应用状态过滤
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // 排除已删除的用户
            query = query.Where(u => !u.IsDeleted);

            // 获取总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<(IEnumerable<Role>, int)> GetUserRolesAsync(UserId userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ([], 0);
            }

            var roles = user.UserRoles.Select(ur => ur.Role);
            return (roles, roles.Count());
        }

        public async Task AssignRolesToUserAsync(UserId userId, IEnumerable<RoleId> roleIds)
        {
            // 获取用户
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new InvalidOperationException("用户不存在");
            }

            // 获取要分配的角色
            var roles = await _dbContext.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();

            // 清除用户当前的所有角色
            user.UserRoles.Clear();

            // 添加新的角色分配
            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole(user, role));
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
