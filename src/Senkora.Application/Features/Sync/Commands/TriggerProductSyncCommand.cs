using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Sync.Commands;

public sealed record TriggerProductSyncCommand(
    Guid TenantId,
    Guid WooStoreId,
    Guid LogoConnectionId,
    bool FullSync = false) : IRequest<Result<Guid>>;
