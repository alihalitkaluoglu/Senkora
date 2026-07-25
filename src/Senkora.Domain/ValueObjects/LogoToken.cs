namespace Senkora.Domain.ValueObjects;

public sealed record LogoToken(
    string AccessToken,
    string? RefreshToken,
    DateTime IssuedAt,
    DateTime ExpiresAt)
{
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsExpiringSoon(int bufferSeconds = 30)
        => DateTime.UtcNow >= ExpiresAt.AddSeconds(-bufferSeconds);
}
