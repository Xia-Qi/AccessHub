using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Domain.Base;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Infrastructure.Database.ModelConfigurations
{
    internal class PermissionConfiguration : BaseEntityTypeConfiguration<Permission>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasConversion(id => id.Value, value => new PermissionId(value));

            builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Description).HasMaxLength(200);
        }
    }
}
