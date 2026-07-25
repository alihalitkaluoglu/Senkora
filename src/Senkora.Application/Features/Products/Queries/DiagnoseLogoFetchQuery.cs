using MediatR;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Queries;

/// <summary>
/// Tani amacli: Logo REST'ten gelen ham yaniti ve parse sonucunu dondurur.
/// Sorun tespitinde kullanilir.
/// </summary>
public sealed record DiagnoseLogoFetchQuery(
    Guid TenantId,
    Guid LogoConnectionId,
    int  Limit = 3) : IRequest<Result<LogoFetchDiagnostics>>;

public sealed record LogoFetchDiagnostics(
    bool    TokenObtained,
    string? TokenPreview,
    string  RequestUrl,
    bool    RequestSucceeded,
    string? RawResponsePreview,
    int     ParsedItemCount,
    string? FirstItemJson,
    string? ErrorMessage,
    string? ErrorStage);

public sealed class DiagnoseLogoFetchQueryHandler(
    ILogoConnectionResolver resolver,
    ILogoDiagnosticsService diagnostics)
    : IRequestHandler<DiagnoseLogoFetchQuery, Result<LogoFetchDiagnostics>>
{
    public async Task<Result<LogoFetchDiagnostics>> Handle(
        DiagnoseLogoFetchQuery request, CancellationToken ct)
    {
        LogoConnectionInfo info;
        try
        {
            info = await resolver.ResolveAsync(
                request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<LogoFetchDiagnostics>.Success(new LogoFetchDiagnostics(
                TokenObtained: false, TokenPreview: null,
                RequestUrl: "", RequestSucceeded: false,
                RawResponsePreview: null, ParsedItemCount: 0, FirstItemJson: null,
                ErrorMessage: ex.Message, ErrorStage: "TOKEN"));
        }

        var result = await diagnostics.ProbeItemsAsync(
            info.RestUrl, info.AccessToken, request.Limit, ct);

        return Result<LogoFetchDiagnostics>.Success(result with
        {
            TokenObtained = true,
            TokenPreview  = info.AccessToken.Length > 24
                ? info.AccessToken[..24] + "..."
                : info.AccessToken
        });
    }
}
