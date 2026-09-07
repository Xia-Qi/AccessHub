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
        public int AccessFailedCount { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>账户锁定阈值:连续失败次数达到此值则锁定。</summary>
        public const int MaxFailedAccessAttempts = 5;
        /// <summary>锁定时长:达到阈值后锁定的时长。</summary>
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        /// <summary>账户当前是否处于锁定状态(LockoutEnd 未过期)。</summary>
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
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

        /// <summary>
        /// 更新密码哈希(用于重置/修复种子脏数据)。
        /// </summary>
        public void UpdatePassword(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Password hash cannot be empty");
            PasswordHash = passwordHash;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// 记录一次登录失败:失败计数 +1,达到阈值则设置 LockoutEnd 锁定账户。
        /// 锁定状态下仍可继续计数(防御式),实际登录流程会先校验 IsLockedOut 拒绝。
        /// </summary>
        public void RecordFailedAccessAttempt()
        {
            AccessFailedCount++;
            if (AccessFailedCount >= MaxFailedAccessAttempts)
            {
                LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            }
        }

        /// <summary>
        /// 登录成功:重置失败计数为 0,清除锁定时间(若有)。
        /// </summary>
        public void ResetAccessFailedCount()
        {
            AccessFailedCount = 0;
            LockoutEnd = null;
        }
        public void SoftDelete()
        {
            if (IsDeleted)
                throw new DomainException("User is already deleted");
            
            IsDeleted = true;
            //AddDomainEvent(new UserDeletedDomainEvent(Id, Name));
        }
    }
}