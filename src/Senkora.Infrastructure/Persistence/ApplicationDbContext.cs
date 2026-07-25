using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Entities.Identity;
using Senkora.Domain.Entities.Integration;
using Senkora.Domain.Entities.Licensing;
using Senkora.Domain.Entities.Orders;
using Senkora.Domain.Entities.Sync;
using Senkora.Domain.Entities.Tenants;

namespace Senkora.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    /// <summary>
    /// true iken AuditInterceptor silme islemlerini soft delete'e cevirmez.
    /// Kalici silme gereken yerlerde kullanilir (orn. urun eslemeleri).
    /// </summary>
    public bool SuppressSoftDelete { get; set; }

    // Master
    public DbSet<Tenant>            Tenants            => Set<Tenant>();
    public DbSet<TenantSettings>    TenantSettings     => Set<TenantSettings>();
    public DbSet<License>           Licenses           => Set<License>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();

    // Identity
    public DbSet<User>              Users              => Set<User>();
    public DbSet<Role>              Roles              => Set<Role>();
    public DbSet<UserRole>          UserRoles          => Set<UserRole>();
    public DbSet<RolePermission>    RolePermissions    => Set<RolePermission>();
    public DbSet<RefreshToken>      RefreshTokens      => Set<RefreshToken>();

    // Integration
    public DbSet<WooStore>          WooStores          => Set<WooStore>();
    public DbSet<LogoConnection>    LogoConnections    => Set<LogoConnection>();
    public DbSet<FieldMapping>      FieldMappings      => Set<FieldMapping>();

    // Sync
    public DbSet<SyncJob>           SyncJobs           => Set<SyncJob>();
    public DbSet<SyncLog>           SyncLogs           => Set<SyncLog>();
    public DbSet<SyncError>         SyncErrors         => Set<SyncError>();

    // Catalog & Orders
    public DbSet<ProductMapping>      ProductMappings      => Set<ProductMapping>();
    public DbSet<ProductSyncHistory>  ProductSyncHistories => Set<ProductSyncHistory>();
    public DbSet<OrderMapping>      OrderMappings      => Set<OrderMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Soft-delete global query filters
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TenantSettings>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WooStore>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LogoConnection>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FieldMapping>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncJob>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncLog>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncError>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProductMapping>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProductSyncHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderMapping>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<License>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LicenseActivation>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
