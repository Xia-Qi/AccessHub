using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class RoleId : ValueObject
    {
        public Guid Value { get; }

        public RoleId(Guid value)
        {
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public static RoleId Create() => new RoleId(Guid.NewGuid());
    }
}