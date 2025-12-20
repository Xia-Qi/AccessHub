using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Infrastructure.Database.ModelConfigurations
{
    internal class RolePermissionConfiguration : BaseEntityTypeConfiguration<RolePermission>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<RolePermission> builder)
        {
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        }
    }
}
