using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Identity;

namespace Senkora.Application.Features.Auth.Commands;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwtService,
    IPasswordHasher passwordHasher,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null)
        {
            logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return Result<LoginResult>.Failure("Kullanici adi veya sifre hatali.", "INVALID_CREDENTIALS");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            return Result<LoginResult>.Failure(
                $"Hesabiniz {user.LockoutEnd:HH:mm} kadar kilitli.", "ACCOUNT_LOCKED");

        if (!user.IsActive)
            return Result<LoginResult>.Failure("Hesabiniz pasif durumda.", "ACCOUNT_INACTIVE");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                logger.LogWarning("Account locked: {Email}", request.Email);
            }
            await db.SaveChangesAsync(ct);
            return Result<LoginResult>.Failure("Kullanici adi veya sifre hatali.", "INVALID_CREDENTIALS");
        }

        if (user.IsMfaEnabled && string.IsNullOrWhiteSpace(request.TotpCode))
            return Result<LoginResult>.Failure("MFA kodu gerekli.", "MFA_REQUIRED");

        user.FailedLoginAttempts = 0;
        user.LockoutEnd          = null;
        user.LastLoginAt         = DateTime.UtcNow;

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .ToList();

        if (user.IsGlobalAdmin && !roles.Contains("SuperAdmin"))
            roles.Add("SuperAdmin");

        var accessToken  = jwtService.GenerateAccessToken(user.Id, user.TenantId, user.Email, roles);
        var refreshToken = jwtService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            TenantId    = user.TenantId,
            UserId      = user.Id,
            Token       = refreshToken,
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress,
            CreatedBy   = user.Id.ToString()
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Login OK: {Email}", user.Email);

        return Result<LoginResult>.Success(new LoginResult(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    DateTime.UtcNow.AddMinutes(60),
            UserId:       user.Id,
            Email:        user.Email,
            FullName:     user.FullName,
            Roles:        roles,
            RequiresMfa:  false));
    }
}
