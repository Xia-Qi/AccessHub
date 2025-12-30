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
    }
}
