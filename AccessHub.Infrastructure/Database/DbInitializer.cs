using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using AccessHub.Domain.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Database
{
    public static class DbInitializer
    {
        public static void Seed(AccessHubDbContext context)
        {
            // 种子用户密码必须经 PBKDF2 哈希存储;此前种子直接把明文 "123" 当哈希写入,
            // 导致登录校验时 FromBase64String 抛 FormatException → 500。此处修正并自愈旧库脏数据。
            var hasher = new DefaultPasswordHasher();
            EnsureSeedUser(context, "admin", "admin@example.com", "123", "13800000001", hasher);
            EnsureSeedUser(context, "test", "test@example.com", "123", "13800000002", hasher);
            context.SaveChanges();

            if (!context.Roles.Any())
            {
                var roles = new List<Role> {
                    new Role("admin","adn"),
                    new Role("user","usr")
                };
                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            if (!context.Permissions.Any())
            {
                var perms = new List<Permission> {
                    new Permission("admin","admin.all",""),
                    new Permission("user","usr.read","")
                };
                context.Permissions.AddRange(perms);
                context.SaveChanges();
            }

            //context.Users.Find(new { Name = "admin" })?.Roles.Add(context.Roles.Find(new { Name = "admin" }));

            //if (!context.RolePermissions.Any())
            //{
            //    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "admin");
            //    var travelerRole = context.Roles.FirstOrDefault(r => r.Name == "user");
            //    var adminPermission = context.Permissions.FirstOrDefault(p => p.Code == "admin.all");

            //    if(adminRole != null && adminPermission != null && travelerRole != null)
            //    {
            //        var pms = new List<RolePermission>
            //        {
            //            new RolePermission(adminRole,adminPermission),
            //            new RolePermission(travelerRole,adminPermission)
            //        };
            //        context.RolePermissions.AddRange(pms);
            //        context.SaveChanges();
            //    }
                
            //}

            //if (!context.UserRoles.Any())
            //{
            //    var adminUser = context.Users.FirstOrDefault(u => u.Name == "admin");
            //    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "admin");
            //    if (adminUser != null && adminRole != null)
            //    {
            //        var userRole = new UserRole(adminUser, adminRole);
            //        context.UserRoles.Add(userRole);
            //        context.SaveChanges();
            //    }
            //}
        }
        /// <summary>
        /// 确保种子用户存在且密码为规范哈希格式。
        /// 新库 → 新建(密码已哈希);旧库脏数据(明文当哈希)→ 自愈重哈希。
        /// </summary>
        private static void EnsureSeedUser(AccessHubDbContext context, string name, string email, string plainPassword, string phone, IPasswordHasher hasher)
        {
            var existing = context.Users.FirstOrDefault(u => u.Name == name);
            var properHash = hasher.HashPassword(plainPassword);
            if (existing == null)
            {
                context.Users.Add(new User(name, email, properHash, phone));
            }
            else if (!IsProperHash(existing.PasswordHash))
            {
                // 自愈:历史脏数据(明文"123"被当哈希存)→ 用规范 PBKDF2 哈希覆盖
                existing.UpdatePassword(properHash);
            }
        }

        /// <summary>
        /// 判断存储的哈希是否为规范格式(salt+hash 的 base64,至少 48 字节)。
        /// </summary>
        private static bool IsProperHash(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            try
            {
                var data = Convert.FromBase64String(stored);
                return data.Length >= 48; // 16 字节 salt + 32 字节 hash
            }
            catch
            {
                return false;
            }
        }

        public static void InitializeDatabase(AccessHubDbContext context)
        {
            // 应用迁移。
            // 注意：迁移前通过 dotnet cli命令生成迁移文件：
            //1. dotnet ef migrations add InitialCreate
            //2. dotnet ef database udpate
            context.Database.Migrate();
            DbInitializer.Seed(context); // 插入种子数据
        }
    }
}
