using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain.Users.Model;
using Domain.Base;

namespace AccessHub.Infrastructure.Database.ModelConfigurations
{
    internal class RoleConfiguration : BaseEntityTypeConfiguration<Role>
    {
        protected override void ConfigureEntity(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasConversion(id => id.Value, value => new RoleId(value));

            builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
            builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        }
    }
}
