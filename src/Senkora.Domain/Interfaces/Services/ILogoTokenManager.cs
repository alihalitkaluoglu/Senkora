using Senkora.Domain.ValueObjects;

namespace Senkora.Domain.Interfaces.Services;

public interface ILogoTokenManager
{
    Task<LogoToken> GetTokenAsync(Guid connectionId, CancellationToken ct = default);
    Task<LogoToken> RefreshTokenAsync(Guid connectionId, CancellationToken ct = default);
    Task RevokeTokenAsync(Guid connectionId, CancellationToken ct = default);
}
