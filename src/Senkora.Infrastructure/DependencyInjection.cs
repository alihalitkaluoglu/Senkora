using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Interfaces.Repositories;
using Senkora.Domain.Interfaces.Services;
using Senkora.Infrastructure.Caching;
using Senkora.Infrastructure.ExternalServices.Logo;
using Senkora.Infrastructure.ExternalServices.WooCommerce;
using Senkora.Infrastructure.Persistence;
using Senkora.Infrastructure.Persistence.Interceptors;
using Senkora.Infrastructure.Persistence.Repositories;
using Senkora.Infrastructure.Security;
using Senkora.Infrastructure.Storage;

namespace Senkora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ── Database ──────────────────────────────────────────────────────
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
            opts.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(
            sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();

        // ── Redis ─────────────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = config["Redis:ConnectionString"]);
        services.AddSingleton<RedisCacheService>();

        // ── Hangfire ──────────────────────────────────────────────────────
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(config.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout       = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout   = TimeSpan.FromMinutes(5),
                    QueuePollInterval            = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks           = true
                }));
        services.AddHangfireServer(opts =>
        {
            opts.Queues      = ["critical", "default", "low-priority"];
            opts.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
        });

        // ── Security ──────────────────────────────────────────────────────
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IJwtTokenService,  JwtTokenServiceImpl>();
        services.AddScoped<IPasswordHasher,   PasswordHasherImpl>();
        services.AddScoped<ICurrentUser,      CurrentUser>();
        services.AddScoped<ICurrentTenant,    CurrentTenant>();

        // ── Licensing ─────────────────────────────────────────────────────
        services.AddScoped<ILicensingService, LicensingService>();
        services.AddScoped<ILicenseValidator, LicenseValidator>();

        // ── Logo REST ─────────────────────────────────────────────────────
        services.AddHttpClient<LogoRestClient>(c =>
        {
            // Logo REST liste sorgulari yavas olabilir (alt tablolar dahil gelir)
            c.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddStandardResilienceHandler(o =>
        {
            o.AttemptTimeout.Timeout      = TimeSpan.FromMinutes(2);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
        });
        services.AddScoped<ILogoTokenManager,        LogoTokenManager>();
        services.AddScoped<ILogoRestService,         LogoRestService>();
        services.AddScoped<ILogoProductService,      LogoProductService>();
        services.AddScoped<ILogoConnectionResolver,  LogoConnectionResolver>();
        services.AddScoped<ILogoDiagnosticsService, LogoDiagnosticsService>();

        // ── WooCommerce ───────────────────────────────────────────────────
        services.AddHttpClient("WooCommerce", c =>
            c.Timeout = TimeSpan.FromSeconds(60))
            .AddStandardResilienceHandler();
        services.AddScoped<IWooCommerceService,      WooCommerceService>();
        services.AddScoped<IWooProductService,       WooProductService>();
        services.AddScoped<IWooConnectionResolver,   WooConnectionResolver>();
        services.AddScoped<IWooMediaService,         WooMediaService>();

        // ── File Storage ──────────────────────────────────────────────────
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
