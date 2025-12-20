using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class RolePermission: Entity
    {
        public Role Role { get; private set; }
        public Permission Permission { get; private set; }
        public RoleId RoleId { get; private set; }
        public PermissionId PermissionId { get; private set; }
        private RolePermission()
        {
        }
        public RolePermission(Role role, Permission permission)
        {
            Role = role;
            Permission = permission;
            RoleId = role.Id;
            PermissionId = permission.Id;
        }
    }
}
