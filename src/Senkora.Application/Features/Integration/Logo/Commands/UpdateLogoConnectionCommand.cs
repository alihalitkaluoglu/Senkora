using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed record UpdateLogoConnectionCommand(
    Guid    Id,
    Guid    TenantId,
    string  Name,
    string  RestUrl,
    string? ClientId,
    string? ClientSecret,
    string? Password,
    int     FirmNo,
    int     PeriodNo,
    int     TimeoutSeconds,
    bool    IsActive)
    : IRequest<Result>;
