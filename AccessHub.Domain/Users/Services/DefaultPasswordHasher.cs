using System;
using System.Linq;
using System.Security.Cryptography;

namespace AccessHub.Domain.Users.Services
{
    /// <summary>
    /// PBKDF2-SHA256 密码哈希器。
    /// 哈希格式(类 Passlib/Django,便于跨语言识别与未来参数升级):
    ///   pbkdf2_sha256$&lt;iterations&gt;$&lt;base64(salt)&gt;$&lt;base64(hash)&gt;
    /// 旧格式兼容:无 '$' 分隔的原始 base64(salt+hash),迭代次数固定 100_000。
    /// 当前迭代次数 600_000(OWASP 2023 对 PBKDF2-SHA256 的最低推荐)。
    /// </summary>
    public class DefaultPasswordHasher : IPasswordHasher
    {
        private const string Algorithm = "pbkdf2_sha256";
        private const char Separator = '$';
        private const int SaltSize = 16;       // 128 bit salt
        private const int HashSize = 32;       // 256 bit hash (SHA-256 输出)
        private const int CurrentIterations = 600_000; // OWASP 2023 推荐
        private const int LegacyIterations = 100_000;  // 旧哈希的迭代次数

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                CurrentIterations,
                HashAlgorithmName.SHA256
            );

            byte[] hash = pbkdf2.GetBytes(HashSize);

            return string.Join(
                Separator,
                Algorithm,
                CurrentIterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash)
            );
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            // 解析哈希:新格式(带 '$')或旧格式(纯 base64)
            if (storedHash.Contains(Separator))
                return VerifyNewFormat(password, storedHash);

            return VerifyLegacyFormat(password, storedHash);
        }

        public bool ShouldRehash(string hash)
        {
            if (string.IsNullOrEmpty(hash) || !hash.Contains(Separator))
                return true; // 旧格式 → 升级

            var parts = hash.Split(Separator);
            // 期望 4 段: algorithm$iterations$salt$hash
            if (parts.Length != 4 || parts[0] != Algorithm)
                return true;

            if (!int.TryParse(parts[1], out var iterations) || iterations < CurrentIterations)
                return true;

            return false;
        }

        private static bool VerifyNewFormat(string password, string storedHash)
        {
            var parts = storedHash.Split(Separator);
            if (parts.Length != 4 || parts[0] != Algorithm)
                return false;

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return false;

            byte[] salt, expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256
            );

            byte[] computed = pbkdf2.GetBytes(HashSize);
            return CryptographicOperations.FixedTimeEquals(expectedHash, computed);
        }

        /// <summary>
        /// 旧格式兼容:原始 base64(salt(16) + hash(32)),迭代次数 100_000。
        /// 用于校验历史种子/脏数据哈希,校验通过后由 ShouldRehash 触发升级。
        /// </summary>
        private static bool VerifyLegacyFormat(string password, string storedHash)
        {
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
            if (data.Length < SaltSize + HashSize)
                return false;

            byte[] salt = data[..SaltSize];
            byte[] expectedHash = data[SaltSize..];

            var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                LegacyIterations,
                HashAlgorithmName.SHA256
            );

            byte[] computed = pbkdf2.GetBytes(HashSize);
            return CryptographicOperations.FixedTimeEquals(expectedHash, computed);
        }
    }
}
