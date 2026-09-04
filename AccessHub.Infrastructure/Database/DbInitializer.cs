using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Infrastructure.Database
{
    public static class DbInitializer
    {
        public static void Seed(AccessHubDbContext context)
        {
            if (!context.Users.Any())
            {
                var users = new List<User> {
                    new User("admin","admin@example.com","123","13800000001"),//{ Name = "admin", Email = "admin@example.com" },
                    new User("test","test@example.com","123","13800000002")
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

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
