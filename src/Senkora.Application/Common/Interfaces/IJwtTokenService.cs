namespace Senkora.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
