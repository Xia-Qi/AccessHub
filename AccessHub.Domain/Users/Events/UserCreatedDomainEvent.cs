using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Domain.Users.Events
{
    public class UserCreatedDomainEvent : IDomainEvent
    {
        public UserId UserId { get; }
        public string Username { get; }
        public DateTime OccurredOn { get; }

        public UserCreatedDomainEvent(UserId userId, string username)
        {
            UserId = userId;
            Username = username;
            OccurredOn = DateTime.UtcNow;
        }
    }
}