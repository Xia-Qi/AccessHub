using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Domain.Base;

namespace AccessHub.Domain.Users.Model
{
    public class PhoneNumber : ValueObject
    {
        private static readonly Regex PhonePattern = new (@"^1[3-9]\d{9}$",RegexOptions.Compiled);
        public PhoneNumber(string value)
        {
            if (string.IsNullOrEmpty(value)){
                throw new DomainException("Phone number not allowed empty!");
            }
            // TODO: testing
            // if(!PhonePattern.IsMatch(value)){
            //     throw new DomainException($"Invalid phone number! {value}");
            // }
            Value = value;
        }

        public string Value { get; private set; }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
