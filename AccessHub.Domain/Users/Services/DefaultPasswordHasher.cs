using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessHub.Domain.Users.Services
{
    public class DefaultPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            throw new NotImplementedException();
        }

        public bool VerifyPassword(string password, string hash)
        {
            throw new NotImplementedException();
        }
    }
}
