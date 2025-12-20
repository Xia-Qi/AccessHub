using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain;
using Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Infrastructure.Database
{
    public static class EntityBuilderExtensions
    {
        /// <summary>
        /// AggregateRoot属性构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityTypeBuilder"></param>
        public static void AggregateRootPropertyBuild(this EntityTypeBuilder entityTypeBuilder)
        {
            // 配置 Version 属性为必需且有默认值
            entityTypeBuilder.Property(nameof(IAggregateRoot.Version))
                .IsRequired()
                .HasDefaultValue(1);
        }

        /// <summary>
        /// 审计属性构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityTypeBuilder"></param>
        public static void AuditablePropertyBuild(this EntityTypeBuilder entityTypeBuilder)
        {
            entityTypeBuilder.Property(nameof(IAuditableEntity.CreatedBy)).IsRequired().HasMaxLength(100);

            entityTypeBuilder.Property(nameof(IAuditableEntity.CreatedAt))
                .IsRequired();

            entityTypeBuilder.Property(nameof(IAuditableEntity.LastModifiedBy)).HasMaxLength(100);
        }

        /// <summary>
        /// 软删除属性构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityTypeBuilder"></param>
        public static void SoftDeletePropertyBuild(this EntityTypeBuilder entityTypeBuilder)
        {
            entityTypeBuilder.Property(nameof(ISoftDelete.IsDeleted)).IsRequired().HasDefaultValue(false);
        }

        /// <summary>
        /// DomainEvents 忽略
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityTypeBuilder"></param>
        public static void EntityEventsPropertyBuild(this EntityTypeBuilder entityTypeBuilder)
        {
            entityTypeBuilder.Ignore(nameof(IEntityWithEvents.DomainEvents));
        }
    }
}
