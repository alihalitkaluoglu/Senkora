using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Senkora.Infrastructure.Caching;

/// <summary>
/// Distributed cache sarmalayici. Redis erisilemezse uygulama calismaya devam eder,
/// sadece cache atlanir (fail-safe).
/// </summary>
public sealed class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger)
{
    private bool _cacheHealthy = true;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_cacheHealthy) return default;
        try
        {
            var data = await cache.GetStringAsync(key, ct);
            return data is null ? default : JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception ex)
        {
            MarkUnhealthy(ex, "GET", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (!_cacheHealthy) return;
        try
        {
            var opts = new DistributedCacheEntryOptions();
            if (expiry.HasValue) opts.SetAbsoluteExpiration(expiry.Value);
            await cache.SetStringAsync(key, JsonConvert.SerializeObject(value), opts, ct);
        }
        catch (Exception ex)
        {
            MarkUnhealthy(ex, "SET", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!_cacheHealthy) return;
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { MarkUnhealthy(ex, "REMOVE", key); }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key, Func<Task<T>> factory,
        TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;
        var value = await factory();
        await SetAsync(key, value, expiry, ct);
        return value;
    }

    private void MarkUnhealthy(Exception ex, string op, string key)
    {
        if (_cacheHealthy)
        {
            logger.LogWarning(ex,
                "Redis erisilemedi ({Op} {Key}). Cache devre disi birakildi, " +
                "uygulama cache olmadan devam edecek.", op, key);
            _cacheHealthy = false;
        }
    }
}
