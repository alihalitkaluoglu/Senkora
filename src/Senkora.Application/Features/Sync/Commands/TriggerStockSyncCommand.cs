using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Sync.Commands;

public sealed record TriggerStockSyncCommand(
    Guid TenantId,
    Guid WooStoreId,
    Guid LogoConnectionId) : IRequest<Result<Guid>>;
