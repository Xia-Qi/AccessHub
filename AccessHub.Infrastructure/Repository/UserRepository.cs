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
            throw new NotImplementedException();
        }

        /// <summary>
        /// The DeleteAsync
        /// </summary>
        /// <param name="aggregate">The aggregate<see cref="User"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task DeleteAsync(User aggregate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The ExistsAsync
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <param name="email">The email<see cref="string"/></param>
        /// <returns>The <see cref="Task{bool}"/></returns>
        public Task<bool> ExistsAsync(string username, string email)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The GetByEmailAsync
        /// </summary>
        /// <param name="email">The email<see cref="string"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public Task<User> GetByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The GetByIdAsync
        /// </summary>
        /// <param name="id">The id<see cref="UserId"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public Task<User> GetByIdAsync(UserId id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The GetByUsernameAsync
        /// </summary>
        /// <param name="username">The username<see cref="string"/></param>
        /// <returns>The <see cref="Task{User}"/></returns>
        public Task<User> GetByUsernameAsync(string username)
        {
            var user = _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefault(x => x.Name == username);

            return Task.FromResult(user);
        }

        /// <summary>
        /// The UpdateAsync
        /// </summary>
        /// <param name="aggregate">The aggregate<see cref="User"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task UpdateAsync(User aggregate)
        {
            throw new NotImplementedException();
        }
    }
}
