using Microsoft.Extensions.Logging;
using Senkora.Domain.Interfaces.Services;
using Senkora.Domain.ValueObjects;
using Senkora.Infrastructure.Caching;
using Senkora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Manages Logo REST access tokens with auto-refresh.
/// Token TTL from Logo docs is ~2 minutes (119s). We refresh 30s before expiry.
/// </summary>
public sealed class LogoTokenManager(
    LogoRestClient client,
    RedisCacheService cache,
    ApplicationDbContext db,
    IEncryptionService encryption,
    ILogger<LogoTokenManager> logger) : ILogoTokenManager
{
    private const int RefreshBufferSeconds = 30;

    public async Task<LogoToken> GetTokenAsync(Guid connectionId, CancellationToken ct = default)
    {
        // 1. Check Redis cache
        var cacheKey = CacheKeys.LogoToken(connectionId);
        var cached = await cache.GetAsync<LogoToken>(cacheKey, ct);
        if (cached is not null && !cached.IsExpiringSoon(RefreshBufferSeconds))
        {
            logger.LogDebug("Logo token from cache for connection {ConnectionId}", connectionId);
            return cached;
        }

        // 2. Try refresh if we have a cached (but expiring) token
        if (cached?.RefreshToken is not null)
        {
            try { return await RefreshTokenAsync(connectionId, ct); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logo token refresh failed, re-authenticating for {ConnectionId}", connectionId);
            }
        }

        // 3. Full re-authentication
        return await AuthenticateAsync(connectionId, ct);
    }

    public async Task<LogoToken> RefreshTokenAsync(Guid connectionId, CancellationToken ct = default)
    {
        // Logo REST does not have a dedicated refresh endpoint — re-authenticate
        return await AuthenticateAsync(connectionId, ct);
    }

    public async Task RevokeTokenAsync(Guid connectionId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.LogoToken(connectionId), ct);
        logger.LogInformation("Logo token revoked for connection {ConnectionId}", connectionId);
    }

    private async Task<LogoToken> AuthenticateAsync(Guid connectionId, CancellationToken ct)
    {
        var conn = await db.LogoConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == connectionId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException($"LogoConnection {connectionId} not found.");

        var clientId     = encryption.Decrypt(conn.ClientIdEncrypted);
        var clientSecret = encryption.Decrypt(conn.ClientSecretEncrypted);
        var password     = encryption.Decrypt(conn.PasswordEncrypted);

        var response = await client.GetTokenAsync(
            conn.RestUrl, clientId, clientSecret,
            conn.Username, password, conn.FirmNo, ct);

        var issuedAt  = DateTime.UtcNow;
        var expiresAt = issuedAt.AddSeconds(response.ExpiresIn);

        var token = new LogoToken(response.AccessToken, response.RefreshToken, issuedAt, expiresAt);

        // Cache with TTL = ExpiresIn - buffer
        var cacheTtl = TimeSpan.FromSeconds(Math.Max(response.ExpiresIn - RefreshBufferSeconds, 10));
        await cache.SetAsync(CacheKeys.LogoToken(connectionId), token, cacheTtl, ct);

        logger.LogInformation("Logo authenticated for connection {ConnectionId}, expires in {Sec}s",
            connectionId, response.ExpiresIn);

        return token;
    }
}
