using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed record DeleteLogoConnectionCommand(Guid Id, Guid TenantId) : IRequest<Result>;

public sealed class DeleteLogoConnectionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteLogoConnectionCommand, Result>
{
    public async Task<Result> Handle(DeleteLogoConnectionCommand request, CancellationToken ct)
    {
        var conn = await db.LogoConnections.FirstOrDefaultAsync(
            c => c.Id == request.Id && c.TenantId == request.TenantId, ct);

        if (conn is null)
            return Result.Failure("Baglanti bulunamadi.", "NOT_FOUND");

        conn.IsDeleted = true;
        conn.IsActive  = false;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
