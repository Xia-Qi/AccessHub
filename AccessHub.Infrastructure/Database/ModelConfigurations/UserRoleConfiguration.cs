using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Infrastructure.Database.ModelConfigurations
{
    internal class UserRoleConfiguration : BaseEntityTypeConfiguration<UserRole>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        }
    }
}
