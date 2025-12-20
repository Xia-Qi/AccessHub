using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AccessHub.Infrastructure.Database
{
    /// <summary>
    /// 用于在保存更改时自动设置审计字段的拦截器
    /// </summary>
    public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var context = eventData.Context;
            var entries = context.ChangeTracker.Entries<IAuditableEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId ?? "system"; // 实现获取当前用户的方法
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModifiedAt = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = _currentUserService.UserId;
                }
            }

            return base.SavingChanges(eventData, result);
        }

    }
}