using MediatR;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed class TestWooConnectionCommandHandler(
    IWooCommerceService wooService,
    ILogger<TestWooConnectionCommandHandler> logger)
    : IRequestHandler<TestWooConnectionCommand, Result<WooConnectionTestResult>>
{
    public async Task<Result<WooConnectionTestResult>> Handle(
        TestWooConnectionCommand request, CancellationToken ct)
    {
        logger.LogInformation("Testing WooCommerce connection: {Url}", request.StoreUrl);

        var result = await wooService.TestConnectionAsync(
            request.StoreUrl, request.ConsumerKey, request.ConsumerSecret, ct);

        return Result<WooConnectionTestResult>.Success(new WooConnectionTestResult(
            IsSuccess:      result.IsSuccess,
            StoreName:      result.StoreName,
            WooVersion:     result.WooVersion,
            ErrorMessage:   result.ErrorMessage,
            ResponseTimeMs: result.ResponseTimeMs));
    }
}
