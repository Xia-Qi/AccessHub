using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class Permission: AggregateRoot<PermissionId>
    {
        public string Code { get; private set;}
        public string Name { get; private set;}
        public string Description { get; private set;}
        public ICollection<RolePermission> RolePermissions { get; } = [];
        private Permission() { }
        public Permission(string code, string name, string description)
        {
            Id = PermissionId.Create();
            Code = code;
            Name = name;
            Description = description;
        }

        public void UpdateCode(string code)
        {
            Code = code;
        }

        public void UpdateName(string name)
        {
            Name = name;
        }

        public void UpdateDescription(string description)
        {
            Description = description;
        }
    }
}
