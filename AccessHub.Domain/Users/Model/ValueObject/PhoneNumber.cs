using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class PhoneNumber : ValueObject
    {
        public PhoneNumber(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            private set
            {
                isValid = IsPhoneNumber();
            }
        }
        public bool IsPhoneNumber()
        {
            return true;
        }
    }
}
