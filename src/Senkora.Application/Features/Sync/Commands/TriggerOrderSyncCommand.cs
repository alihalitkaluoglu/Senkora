using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Sync.Commands;

public sealed record TriggerOrderSyncCommand(
    Guid TenantId,
    Guid WooStoreId,
    Guid LogoConnectionId,
    long? SpecificOrderId = null) : IRequest<Result<Guid>>;
