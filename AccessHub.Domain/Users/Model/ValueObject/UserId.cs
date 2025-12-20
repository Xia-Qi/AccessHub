using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class UserId : ValueObject
    {
        public Guid Value { get; }

        public UserId(Guid value)
        {
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public static UserId Create() => new UserId(Guid.NewGuid());
    }
}