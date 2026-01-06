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
        //public ICollection<User> Users { get; } = [];//��������д���ο�User��Roles��ע��
        public ICollection<UserRole> UserRoles { get; } = [];

        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("角色名称不能为空");
            
            Name = name;
        }

        public void UpdateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new DomainException("角色编码不能为空");
            
            Code = code;
        }
    }
}