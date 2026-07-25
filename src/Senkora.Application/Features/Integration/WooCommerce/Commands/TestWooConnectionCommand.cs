using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed record TestWooConnectionCommand(
    string StoreUrl,
    string ConsumerKey,
    string ConsumerSecret) : IRequest<Result<WooConnectionTestResult>>;

public sealed record WooConnectionTestResult(
    bool IsSuccess,
    string? StoreName,
    string? WooVersion,
    string? ErrorMessage,
    long ResponseTimeMs);
