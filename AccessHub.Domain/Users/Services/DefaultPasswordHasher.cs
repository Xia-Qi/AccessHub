using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AccessHub.Domain.Users.Services
{
    public class DefaultPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256
            );

            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(
                salt.Concat(hash).ToArray()
            );
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] data = Convert.FromBase64String(storedHash);

            byte[] salt = data[..16];
            byte[] hash = data[16..];

            var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256
            );

            byte[] computed = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(hash, computed);
        }
    }
}
