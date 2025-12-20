using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class UserRole: Entity
    {
        public User User { get; private set; }
        public Role Role { get; private set; }

        public UserId UserId { get; private set; } = null!; // many-to-many should not be null
        public RoleId RoleId { get; private set; } = null!;

        private UserRole() { }
        public UserRole(User user, Role role)
        {
            User = user;
            Role = role;
            UserId = user.Id;
            RoleId = role.Id;
        }

    }
}
