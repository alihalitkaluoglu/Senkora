using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Domain.Entities.Identity;
using Senkora.Domain.Entities.Tenants;
using Senkora.Domain.Enums;

namespace Senkora.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await SeedSystemTenantAsync(db, logger);
        await SeedSystemRolesAsync(db, logger);
        await SeedSuperAdminAsync(db, logger);
    }

    private static async Task SeedSystemTenantAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Tenants.AnyAsync(t => t.Subdomain == "system")) return;

        db.Tenants.Add(new Tenant
        {
            Id                 = new Guid("00000000-0000-0000-0000-000000000001"),
            Name               = "System",
            Subdomain          = "system",
            ContactEmail       = "admin@senkora.io",
            ContactPhone       = "",
            IsActive           = true,
            LicenseTier        = LicenseTier.Enterprise,
            MaxWooStores       = 999,
            MaxLogoConnections = 999,
            MaxMarketplaces    = 999,
            CreatedBy          = "seed"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("System tenant seeded");
    }

    private static async Task SeedSystemRolesAsync(ApplicationDbContext db, ILogger logger)
    {
        var systemTenantId = new Guid("00000000-0000-0000-0000-000000000001");
        var roleNames = new[]
        {
            ("SuperAdmin",  "Platform-wide full access"),
            ("TenantAdmin", "Full access within tenant"),
            ("SyncManager", "Manage sync jobs and connections"),
            ("Viewer",      "Read-only access"),
        };

        foreach (var (name, desc) in roleNames)
        {
            if (await db.Roles.AnyAsync(r => r.TenantId == systemTenantId && r.Name == name)) continue;
            db.Roles.Add(new Role
            {
                TenantId     = systemTenantId,
                Name         = name,
                Description  = desc,
                IsSystemRole = true,
                CreatedBy    = "seed"
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("System roles seeded");
    }

    private static async Task SeedSuperAdminAsync(ApplicationDbContext db, ILogger logger)
    {
        var systemTenantId = new Guid("00000000-0000-0000-0000-000000000001");
        const string adminEmail = "admin@senkora.io";

        if (await db.Users.AnyAsync(u => u.Email == adminEmail)) return;

        var superAdminRole = await db.Roles
            .FirstAsync(r => r.TenantId == systemTenantId && r.Name == "SuperAdmin");

        var adminUser = new User
        {
            TenantId      = systemTenantId,
            Email         = adminEmail,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword("Admin@Senkora2024!", workFactor: 12),
            FirstName     = "Super",
            LastName      = "Admin",
            IsActive      = true,
            IsGlobalAdmin = true,
            CreatedBy     = "seed"
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole
        {
            TenantId  = systemTenantId,
            UserId    = adminUser.Id,
            RoleId    = superAdminRole.Id,
            CreatedBy = "seed"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("SuperAdmin seeded: {Email}", adminEmail);
    }
}
