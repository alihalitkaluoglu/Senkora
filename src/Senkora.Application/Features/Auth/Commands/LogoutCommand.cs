using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
