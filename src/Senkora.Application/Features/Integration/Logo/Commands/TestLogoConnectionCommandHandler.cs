using MediatR;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Commands;

public sealed class TestLogoConnectionCommandHandler(
    ILogoRestService logoService,
    ILogger<TestLogoConnectionCommandHandler> logger)
    : IRequestHandler<TestLogoConnectionCommand, Result<LogoConnectionTestResult>>
{
    public async Task<Result<LogoConnectionTestResult>> Handle(
        TestLogoConnectionCommand request, CancellationToken ct)
    {
        logger.LogInformation("Testing Logo connection: {Url}", request.RestUrl);

        var result = await logoService.TestConnectionAsync(
            request.RestUrl, request.ClientId, request.ClientSecret,
            request.Username, request.Password, request.FirmNo, ct);

        return Result<LogoConnectionTestResult>.Success(new LogoConnectionTestResult(
            IsSuccess:      result.IsSuccess,
            AccessToken:    result.AccessToken,
            CurrentFirm:    result.CurrentFirm,
            ErrorMessage:   result.ErrorMessage,
            ResponseTimeMs: result.ResponseTimeMs));
    }
}
