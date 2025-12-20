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
    internal class UserConfiguration : BaseEntityTypeConfiguration<User>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasConversion(id => id.Value, value => new UserId(value));

            builder.Property(u => u.Name).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.PhoneNumber).HasConversion(p => p.Value, value => new PhoneNumber(value));

        }
    }
}
