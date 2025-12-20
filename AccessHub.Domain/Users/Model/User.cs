using AccessHub.Domain.Users.Events;
using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class User : AggregateRoot<UserId>, ISoftDelete
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public PhoneNumber PhoneNumber {  get; private set; }
        public DateTime? LockoutEnd { get; private set; }
        public bool IsActive { get; private set; }
        //当你使用“自定义中间表实体(UserRole)”时，EF Core 不允许你再直接做 Many-to-Many，它会退化为两个 One-to-Many + One-to-Many。
        //这时，再包含该Roles，会出现模型冲突（EF 不知道你是要自动多对多还是手动多对多）。替换为中间表UserRoles.
        //public ICollection<Role> Roles { get; } = [];
        public ICollection<UserRole> UserRoles { get; } = [];
        public bool IsDeleted { get; set; }

        private User() { }//EF reach out

        /// <summary>
        /// 创建新的user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="passwordHash"></param>
        /// <param name="phoneNumber"></param>
        public User(string username, string email, string passwordHash,string phoneNumber)
        {
            Id = UserId.Create();
            Name = username;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = true;
            IsDeleted = false;
            PhoneNumber = new PhoneNumber(phoneNumber);

            //AddDomainEvent(new UserCreatedDomainEvent(Id, username));
        }

        public bool ValidatePassword(string rawPassword)
        {
            return rawPassword == PasswordHash;
            //return BCrypt.Net.BCrypt.Verify(rawPassword, PasswordHash);
        }

    }
}