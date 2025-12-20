using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class PermissionId:ValueObject
    {
        public Guid Value { get; }

        public PermissionId(Guid value)
        {
            Value = value;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public static PermissionId Create() => new PermissionId(Guid.NewGuid());
    }
}
