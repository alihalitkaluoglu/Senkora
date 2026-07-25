using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed record CreateWooStoreCommand(
    Guid   TenantId,
    string Name,
    string StoreUrl,
    string ConsumerKey,
    string  ConsumerSecret,
    string? WpUsername    = null,
    string? WpAppPassword = null)
    : IRequest<Result<Guid>>;
