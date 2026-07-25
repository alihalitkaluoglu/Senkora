using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed record TestLogoConnectionCommand(
    string RestUrl,
    string ClientId,
    string ClientSecret,
    string Username,
    string Password,
    int FirmNo) : IRequest<Result<LogoConnectionTestResult>>;

public sealed record LogoConnectionTestResult(
    bool IsSuccess,
    string? AccessToken,
    int? CurrentFirm,
    string? ErrorMessage,
    long ResponseTimeMs);
