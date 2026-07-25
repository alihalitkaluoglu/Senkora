using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Senkora.Application.Common.Exceptions;
using Senkora.Domain.Exceptions;

namespace Senkora.Api.Middleware;

/// <summary>
/// Yakalanmamis tum hatalari standart ProblemDetails formatinda dondurur.
/// Gelistirme ortaminda inner exception zinciri de gonderilir.
/// </summary>
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", Flatten(ex));
            await WriteAsync(context, ex, _env.IsDevelopment());
        }
    }

    /// <summary>Inner exception zincirini tek satirda birlestirir.</summary>
    private static string Flatten(Exception ex)
    {
        var sb    = new StringBuilder();
        var cur   = ex;
        var depth = 0;

        while (cur != null && depth < 6)
        {
            if (depth > 0) sb.Append("  ->  ");
            sb.Append(cur.Message);
            cur = cur.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    private static async Task WriteAsync(
        HttpContext context, Exception exception, bool isDev)
    {
        context.Response.ContentType = "application/problem+json";

        HttpStatusCode status;
        string         title;
        string[]       errors;

        switch (exception)
        {
            case ValidationException ve:
                status = HttpStatusCode.UnprocessableEntity;
                title  = "Dogrulama hatasi";
                errors = ve.Errors.SelectMany(e => e.Value).ToArray();
                break;

            case NotFoundException nfe:
                status = HttpStatusCode.NotFound;
                title  = nfe.Message;
                errors = Array.Empty<string>();
                break;

            case ForbiddenException fe:
                status = HttpStatusCode.Forbidden;
                title  = fe.Message;
                errors = Array.Empty<string>();
                break;

            case LicenseException le:
                status = HttpStatusCode.PaymentRequired;
                title  = le.Message;
                errors = Array.Empty<string>();
                break;

            case DomainException de:
                status = HttpStatusCode.BadRequest;
                title  = de.Message;
                errors = Array.Empty<string>();
                break;

            case UnauthorizedAccessException:
                status = HttpStatusCode.Unauthorized;
                title  = "Yetkisiz erisim";
                errors = Array.Empty<string>();
                break;

            default:
                status = HttpStatusCode.InternalServerError;
                title  = Flatten(exception);
                errors = Array.Empty<string>();
                break;
        }

        context.Response.StatusCode = (int)status;

        // Gelistirme ortaminda gercek hatayi da gonder
        var errorList = errors.Length > 0
            ? errors
            : (isDev ? new[] { Flatten(exception) } : Array.Empty<string>());

        var problem = new
        {
            type    = "https://httpstatuses.com/" + (int)status,
            title,
            status  = (int)status,
            errors  = errorList,
            detail  = isDev ? exception.ToString() : null,
            traceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
}
