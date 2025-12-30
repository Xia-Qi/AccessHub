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
        //����ʹ�á��Զ����м��ʵ��(UserRole)��ʱ��EF Core ����������ֱ���� Many-to-Many�������˻�Ϊ���� One-to-Many + One-to-Many��
        //��ʱ���ٰ�����Roles�������ģ�ͳ�ͻ��EF ��֪������Ҫ�Զ���Զ໹���ֶ���Զࣩ���滻Ϊ�м��UserRoles.
        //public ICollection<Role> Roles { get; } = [];
        public ICollection<UserRole> UserRoles { get; } = [];
        public bool IsDeleted { get; set; }

        private User() { }//EF reach out

        /// <summary>
        /// �����µ�user
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
        public void UpdateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email cannot be empty");
            
            Email = email;
        }

        public void UpdatePhoneNumber(string phoneNumber)
        {
            PhoneNumber = new PhoneNumber(phoneNumber);
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
        public void SoftDelete()
        {
            if (IsDeleted)
                throw new DomainException("User is already deleted");
            
            IsDeleted = true;
            AddDomainEvent(new UserDeletedDomainEvent(Id, Name));
        }
    }
}