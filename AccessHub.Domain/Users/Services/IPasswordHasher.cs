namespace AccessHub.Domain.Users.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// 检测存储的哈希是否需要升级到当前算法参数(迭代次数/格式)。
        /// 登录成功后调用,若为 true 则用 HashPassword 重新哈希并持久化,
        /// 实现旧哈希的惰性升级,无需批量迁移。
        /// </summary>
        bool ShouldRehash(string hash);
    }
}