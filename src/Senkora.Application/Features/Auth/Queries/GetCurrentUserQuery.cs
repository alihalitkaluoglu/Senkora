using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;

public sealed record CurrentUserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName,
    IEnumerable<string> Roles,
    bool IsGlobalAdmin,
    DateTime? LastLoginAt);

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result<CurrentUserDto>.Failure("Kimlik dogrulanmadi.", "UNAUTHORIZED");

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);

        if (user is null)
            return Result<CurrentUserDto>.Failure("Kullanici bulunamadi.", "NOT_FOUND");

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .ToList();

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            UserId:       user.Id,
            TenantId:     user.TenantId,
            Email:        user.Email,
            FullName:     user.FullName,
            Roles:        roles,
            IsGlobalAdmin:user.IsGlobalAdmin,
            LastLoginAt:  user.LastLoginAt));
    }
}
