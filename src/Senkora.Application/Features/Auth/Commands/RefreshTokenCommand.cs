using MediatR;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Auth.Commands;

namespace Senkora.Application.Features.Auth.Commands;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<Result<LoginResult>>;
