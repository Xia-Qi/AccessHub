using System;
using System.Collections.Generic;
using System.Linq;
using AccessHub.Domain.Users.Model;
using AccessHub.Domain.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Database
{
    /// <summary>
    /// 系统引导种子:admin 用户 + admin 角色 + 细粒度权限集 + 通配符。
    /// 命名规范(借鉴 Microsoft Graph + AWS IAM):
    ///   permission: 资源.动作(user.read) | 模块通配(user.all) | 顶级通配(*.* 仅 admin)
    ///   scope: ahb.模块(ahb.usermgmt) — 在 OpenIddictSetup 种子,不在此处
    /// admin 拥有 *.*(顶级通配,PolicyProvider 短路所有具体权限)。
    /// </summary>
    public static class DbInitializer
    {
        public static void Seed(AccessHubDbContext context)
        {
            var hasher = new DefaultPasswordHasher();

            // 1. 权限种子:细粒度资源.动作 + 模块通配(.all)+ 顶级通配(*.*)
            var permissionDefs = new (string Code, string Name, string Description)[]
            {
                // 顶级通配(仅系统引导 admin 用,谨慎)
                ("*.*",            "Super Admin",        "All permissions wildcard"),
                // 模块级通配(给模块管理员角色用)
                ("user.all",       "User Module Admin",  "All user module permissions"),
                ("client.all",     "Client Module Admin","All client module permissions"),
                ("role.all",       "Role Module Admin",  "All role module permissions"),
                // 用户模块细粒度
                ("user.read",      "User Read",          "Read user"),
                ("user.list",      "User List",          "List users"),
                ("user.write",     "User Write",         "Create/Update user"),
                ("user.delete",    "User Delete",        "Delete user"),
                // 客户端模块细粒度
                ("client.read",    "Client Read",        "Read client"),
                ("client.write",   "Client Write",       "Create/Update client and grant permissions"),
                ("client.delete",  "Client Delete",      "Delete client"),
                // 角色模块细粒度
                ("role.read",      "Role Read",          "Read role"),
                ("role.write",     "Role Write",          "Create/Update role and assign permissions"),
                ("role.delete",    "Role Delete",        "Delete role"),
            };
            var permById = new Dictionary<string, Permission>();
            foreach (var (code, name, desc) in permissionDefs)
            {
                var perm = context.Permissions.FirstOrDefault(p => p.Code == code);
                if (perm == null)
                {
                    perm = new Permission(code, name, desc);
                    context.Permissions.Add(perm);
                    context.SaveChanges();
                }
                permById[code] = perm;
            }

            // 2. 角色:admin → [*.*](顶级通配,系统引导)
            var adminRole = context.Roles.Include(r => r.RolePermissions).FirstOrDefault(r => r.Name == "admin");
            if (adminRole == null)
            {
                adminRole = new Role("admin", "adn");
                context.Roles.Add(adminRole);
                context.SaveChanges();
                adminRole = context.Roles.Include(r => r.RolePermissions).First(r => r.Name == "admin");
            }
            var superPerm = permById["*.*"];
            if (!adminRole.RolePermissions.Any(rp => rp.PermissionId.Equals(superPerm.Id)))
            {
                adminRole.RolePermissions.Add(new RolePermission(adminRole, superPerm));
                context.SaveChanges();
            }

            // 3. 用户:admin(密码哈希规范自愈),绑定 admin 角色
            EnsureSeedUser(context, "admin", "admin@example.com", "123", "13800000001", hasher);
            var adminUser = context.Users.Include(u => u.UserRoles).FirstOrDefault(u => u.Name == "admin");
            if (adminUser != null && !adminUser.UserRoles.Any(ur => ur.RoleId.Equals(adminRole.Id)))
            {
                adminUser.UserRoles.Add(new UserRole(adminUser, adminRole));
                context.SaveChanges();
            }
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
            context.SaveChanges();
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
            context.Database.Migrate();
            DbInitializer.Seed(context);
        }
    }
}
