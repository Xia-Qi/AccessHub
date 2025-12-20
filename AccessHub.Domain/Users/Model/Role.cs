using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class Role : AggregateRoot<RoleId>
    {
        private Role() { }
        public Role(string name,string code)
        {
            Id = RoleId.Create();
            Name = name;
            Code = code;
        }
        public string Name { get;private set; }
        public string Code { get;private set; }
        public ICollection<RolePermission> RolePermissions { get; } = [];
        //public ICollection<User> Users { get; } = [];//不能这样写，参考User里Roles的注释
        public ICollection<UserRole> UserRoles { get; } = [];

    }
}