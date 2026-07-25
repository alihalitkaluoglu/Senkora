using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed record DeleteWooStoreCommand(Guid Id, Guid TenantId) : IRequest<Result>;

public sealed class DeleteWooStoreCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteWooStoreCommand, Result>
{
    public async Task<Result> Handle(DeleteWooStoreCommand request, CancellationToken ct)
    {
        var store = await db.WooStores.FirstOrDefaultAsync(
            s => s.Id == request.Id && s.TenantId == request.TenantId, ct);

        if (store is null)
            return Result.Failure("Magaza bulunamadi.", "NOT_FOUND");

        store.IsDeleted = true;
        store.IsActive  = false;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
