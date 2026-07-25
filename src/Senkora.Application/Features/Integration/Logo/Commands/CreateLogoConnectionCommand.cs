using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed record CreateLogoConnectionCommand(
    Guid   TenantId,
    string Name,
    string RestUrl,
    string ClientId,
    string ClientSecret,
    string Username,
    string Password,
    int    FirmNo,
    int    PeriodNo = 1,
    int    TimeoutSeconds = 30)
    : IRequest<Result<Guid>>;
