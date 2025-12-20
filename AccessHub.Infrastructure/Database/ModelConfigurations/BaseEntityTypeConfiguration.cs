using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessHub.Domain;
using Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Infrastructure.Database.ModelConfigurations
{
    internal abstract class BaseEntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : class
    {
        /// <summary>
        /// 统一配置基类属性。这里显式实现：子类看不到、不能 override、不能调用，子类只能重写ConfigureEntity方法
        /// </summary>
        /// <param name="builder"></param>
        void IEntityTypeConfiguration<T>.Configure(EntityTypeBuilder<T> builder)
        {
            if(typeof(IAggregateRoot).IsAssignableFrom(typeof(T)))
            {
                builder.AggregateRootPropertyBuild();
            }

            if (typeof(IAuditableEntity).IsAssignableFrom(typeof(T)))
            {
                builder.AuditablePropertyBuild();
            }

            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                builder.SoftDeletePropertyBuild();
            }

            if (typeof(IEntityWithEvents).IsAssignableFrom(typeof(T)))
            {
                builder.EntityEventsPropertyBuild();
            }

            // 让子类写专属配置，但不能改整体流程
            ConfigureEntity(builder);
        }

        //子类实现这个方法，配置实体的专有属性
        protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);

    }
}
