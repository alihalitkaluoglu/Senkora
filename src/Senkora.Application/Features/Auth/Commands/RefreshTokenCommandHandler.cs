using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Identity;

namespace Senkora.Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwtService,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var existing = await db.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (existing is null || !existing.IsActive)
        {
            logger.LogWarning("Invalid refresh token");
            return Result<LoginResult>.Failure("Gecersiz token.", "INVALID_TOKEN");
        }

        var user = existing.User;
        if (!user.IsActive)
            return Result<LoginResult>.Failure("Hesabiniz pasif.", "ACCOUNT_INACTIVE");

        existing.IsRevoked       = true;
        existing.RevokedAt       = DateTime.UtcNow;

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .ToList();

        if (user.IsGlobalAdmin && !roles.Contains("SuperAdmin"))
            roles.Add("SuperAdmin");

        var newAccessToken  = jwtService.GenerateAccessToken(user.Id, user.TenantId, user.Email, roles);
        var newRefreshToken = jwtService.GenerateRefreshToken();
        existing.ReplacedByToken = newRefreshToken;

        db.RefreshTokens.Add(new RefreshToken
        {
            TenantId    = user.TenantId,
            UserId      = user.Id,
            Token       = newRefreshToken,
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress,
            CreatedBy   = user.Id.ToString()
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Token refreshed: {Email}", user.Email);

        return Result<LoginResult>.Success(new LoginResult(
            AccessToken:  newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt:    DateTime.UtcNow.AddMinutes(60),
            UserId:       user.Id,
            Email:        user.Email,
            FullName:     user.FullName,
            Roles:        roles,
            RequiresMfa:  false));
    }
}
