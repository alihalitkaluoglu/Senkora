using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? TotpCode,
    string IpAddress) : IRequest<Result<LoginResult>>;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    IEnumerable<string> Roles,
    bool RequiresMfa);
