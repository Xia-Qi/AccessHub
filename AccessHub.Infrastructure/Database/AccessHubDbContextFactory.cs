using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AccessHub.Infrastructure.Database
{
    /// <summary>
    /// 用于在设计时创建 AccessHubDbContext 的工厂类,程序运行中不会调用
    /// </summary>
    public class AccessHubDbContextFactory : IDesignTimeDbContextFactory<AccessHubDbContext>
    {
        public AccessHubDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AccessHubDbContext>();
            // 替换为你的 MySQL 连接字符串
            optionsBuilder.UseMySql(
                "Server=localhost;Database=test222;User=root;Password=123456;SSLMode=none",
                new MySqlServerVersion(new Version(8, 0, 36))
            );

            // 设计时不需要拦截器，可传 null 或 new DummyInterceptor()
            return new AccessHubDbContext(optionsBuilder.Options, new DummyInterceptor());
        }
    }

    // 可选：一个空实现，避免设计时出错
    public class DummyInterceptor : IInterceptor { }
}
