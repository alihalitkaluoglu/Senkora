using Microsoft.EntityFrameworkCore;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Entities.Identity;
using Senkora.Domain.Entities.Integration;
using Senkora.Domain.Entities.Licensing;
using Senkora.Domain.Entities.Orders;
using Senkora.Domain.Entities.Sync;
using Senkora.Domain.Entities.Tenants;

namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Application katmaninin veritabani erisimi icin kullandigi soyutlama.
/// Infrastructure katmanindaki ApplicationDbContext bu interface'i implemente eder.
/// </summary>
public interface IApplicationDbContext
{
    // Master
    DbSet<Tenant>            Tenants            { get; }
    DbSet<TenantSettings>    TenantSettings     { get; }
    DbSet<License>           Licenses           { get; }
    DbSet<LicenseActivation> LicenseActivations { get; }

    // Identity
    DbSet<User>              Users              { get; }
    DbSet<Role>              Roles              { get; }
    DbSet<UserRole>          UserRoles          { get; }
    DbSet<RolePermission>    RolePermissions    { get; }
    DbSet<RefreshToken>      RefreshTokens      { get; }

    // Integration
    DbSet<WooStore>          WooStores          { get; }
    DbSet<LogoConnection>    LogoConnections    { get; }
    DbSet<FieldMapping>      FieldMappings      { get; }

    // Sync
    DbSet<SyncJob>           SyncJobs           { get; }
    DbSet<SyncLog>           SyncLogs           { get; }
    DbSet<SyncError>         SyncErrors         { get; }

    // Catalog & Orders
    DbSet<ProductMapping>       ProductMappings       { get; }
    DbSet<ProductSyncHistory>   ProductSyncHistories  { get; }
    DbSet<OrderMapping>      OrderMappings      { get; }

    /// <summary>
    /// true yapilirsa bu SaveChanges cagrisinda silme islemleri
    /// soft delete'e cevrilmez, kayitlar veritabanindan kalici olarak silinir.
    /// Islem sonunda otomatik olarak false'a doner.
    /// </summary>
    bool SuppressSoftDelete { get; set; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
