using AccessHub.Domain.Users.Model;
using AccessHub.Domain;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;
using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Base;
using OpenIddict.EntityFrameworkCore.Models;

namespace AccessHub.Infrastructure.Database
{
    public class AccessHubDbContext : DbContext
    {
        private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;
        public AccessHubDbContext(DbContextOptions<AccessHubDbContext> options, AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor) : base(options)
        {
            _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
        }
        public DbSet<User> Users { get; set; } //TODO: 同public DbSet<User> Users => Set<User>()写法的区别
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        //public DbSet<RolePermission> RolePermissions { get; set; }

        //public DbSet<UserRole> UserRoles { get; set; }

        // OpenIddict 使用的表模型
        public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIdApplications => Set<OpenIddictEntityFrameworkCoreApplication>();
        public DbSet<OpenIddictEntityFrameworkCoreAuthorization> OpenIdAuthorizations => Set<OpenIddictEntityFrameworkCoreAuthorization>();
        public DbSet<OpenIddictEntityFrameworkCoreScope> OpenIdScopes => Set<OpenIddictEntityFrameworkCoreScope>();
        public DbSet<OpenIddictEntityFrameworkCoreToken> OpenIdTokens => Set<OpenIddictEntityFrameworkCoreToken>();
        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDelete);

            foreach (var entry in entries)
            {
                entry.State = EntityState.Modified;
                ((ISoftDelete)entry.Entity).IsDeleted = true;
            }
            return base.SaveChanges();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //分组配置model
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessHubDbContext).Assembly);

            modelBuilder.UseOpenIddict();

            modelBuilder.Entity<OpenIddictEntityFrameworkCoreToken>(entity =>
            {
                entity.Property(t => t.Status)
                    .HasMaxLength(50);

                entity.Property(t => t.Subject)
                    .HasMaxLength(255);

                entity.Property(t => t.Type)
                    .HasMaxLength(100);
            });
            modelBuilder.Entity<OpenIddictEntityFrameworkCoreApplication>(entity =>
            {
                entity.Property(a => a.ClientId)
                    .HasMaxLength(100);

                entity.Property(a => a.ClientSecret)
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<OpenIddictEntityFrameworkCoreScope>(entity =>
            {
                entity.Property(s => s.Name)
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<OpenIddictEntityFrameworkCoreAuthorization>(entity =>
            {
                entity.Property(a => a.Subject)
                    .HasMaxLength(255);
                entity.Property(a => a.Type)
                    .HasMaxLength(100);
            });


            //集中配置model
            //modelBuilder.Entity<User>(entity =>
            //{
            //    entity.HasKey(u => u.Id);
            //    entity.Property(u => u.Id).HasConversion(id => id.Value, value => new UserId(value));
            //    entity.Property(u => u.Name).IsRequired().HasMaxLength(50);
            //    entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            //    entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
            //    entity.Property(u => u.IsActive).IsRequired();
            //    entity.Property(u => u.PhoneNumber).HasConversion(p => p.Value, value => new PhoneNumber(value));
            //    entity.Ignore(u => u.DomainEvents);
            //    entity.AggregateRootPropertyBuild();
            //    entity.AuditablePropertyBuild();
            //    entity.SoftDeletePropertyBuild();
            //});

            //modelBuilder.Entity<Role>().HasMany(u => u.Users).WithMany(r => r.Roles);
            //modelBuilder.Entity<User>().HasMany(u => u.Roles).WithMany(r => r.Users);
            //.UsingEntity<UserRole>(
            //j => j
            //    .HasOne(ur => ur.Role)
            //    .WithMany()
            //    .HasForeignKey(ur => ur.RoleId),
            //j => j
            //    .HasOne(ur => ur.User)
            //    .WithMany()
            //    .HasForeignKey(ur => ur.UserId),
            //j =>
            //{
            //    j.ToTable("UserRole");
            //    j.HasKey(ur => new { ur.UserId, ur.RoleId });
            //}
            //);

            //modelBuilder.Entity<UserRole>(entity =>
            //{
            //    entity.ToTable("UserRole");

            //    entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            //    entity.HasOne(ur => ur.User)
            //        .WithMany()
            //        .HasForeignKey(ur => ur.UserId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasOne(ur => ur.Role)
            //        .WithMany()
            //        .HasForeignKey(ur => ur.RoleId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.AuditablePropertyBuild();
            //});

            //modelBuilder.Entity<Role>(entity =>
            //{
            //    entity.HasKey(r => r.Id);
            //    entity.Property(r => r.Id).HasConversion(id => id.Value, value => new RoleId(value));
            //    entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            //    entity.Property(r=>r.Code).IsRequired().HasMaxLength(50);

            //    entity.AggregateRootPropertyBuild();
            //    entity.AuditablePropertyBuild();
            //});

            //modelBuilder.Entity<Permission>(entity =>
            //{ 
            //    entity.HasKey(p => p.Id);
            //    entity.Property(p => p.Id).HasConversion(id => id.Value, value => new PermissionId(value));
            //    entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            //    entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
            //    entity.Property(p => p.Description).HasMaxLength(200);

            //    entity.AggregateRootPropertyBuild();
            //    entity.AuditablePropertyBuild();
            //});

            //modelBuilder.Entity<Permission>().HasMany(r => r.Roles).WithMany(p => p.Permissions);
            //modelBuilder.Entity<Role>().HasMany(r => r.Permissions).WithMany(p => p.Roles);
            //.UsingEntity<RolePermission>(
            //j => j
            //    .HasOne(r => r.Permission)
            //    .WithMany()
            //    .HasForeignKey(p => p.RoleId),
            //j => j
            //    .HasOne(p => p.Role)
            //    .WithMany()
            //    .HasForeignKey(r => r.PermissionId),
            //j =>
            //{
            //    j.ToTable("UserRole");
            //    j.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            //}
            //);

            //modelBuilder.Entity<RolePermission>(entity =>
            //{
            //    entity.ToTable("RolePermission");

            //    entity.HasKey(rp => new { rp.RoleId, rp.PermissionId});

            //    entity.HasOne(rp => rp.Role)
            //        .WithMany()
            //        .HasForeignKey(rp => rp.RoleId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasOne(rp => rp.Permission)
            //        .WithMany()
            //        .HasForeignKey(rp => rp.PermissionId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.AuditablePropertyBuild();
            //});

            // 为所有实现 ISoftDelete 的实体配置全局过滤器
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(AccessHubDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(null, new object[] { modelBuilder });
                }
            }
        }


        /// <summary>
        /// 那么，如何查询已删除的？如下
        //    /// var deletedUsers = context.Users
        //.IgnoreQueryFilters() // 禁用全局过滤器
        //.Where(u => u.IsDeleted)
        //.ToList();
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="modelBuilder"></param>
        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ISoftDelete
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted); //过滤已删除的实体
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
            base.OnConfiguring(optionsBuilder);
        }
    }
}