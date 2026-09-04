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
            // 防御:存储的可能是明文或脏数据(非规范 base64 哈希),视为校验失败而非抛异常,
            // 避免 login 流程因脏数据返回 500。规范哈希由 HashPassword 产出(salt+hash 的 base64)。
            byte[] data;
            try
            {
                data = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }
            // 16 字节 salt + 32 字节 hash = 48 字节;不达长度视为脏数据
            if (data.Length < 48)
                return false;

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
