using System.Net;
using System.Text.Json;
using Senkora.Application.Common.Exceptions;
using Senkora.Domain.Exceptions;

namespace Senkora.Api.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, env.IsDevelopment());
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, Exception exception, bool isDev)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve  => (HttpStatusCode.UnprocessableEntity, "Dogrulama hatasi", ve.Errors.SelectMany(e => e.Value).ToArray()),
            NotFoundException nfe   => (HttpStatusCode.NotFound,            nfe.Message,        Array.Empty<string>()),
            ForbiddenException fe   => (HttpStatusCode.Forbidden,           fe.Message,         Array.Empty<string>()),
            LicenseException le     => (HttpStatusCode.PaymentRequired,     le.Message,         Array.Empty<string>()),
            DomainException de      => (HttpStatusCode.BadRequest,          de.Message,         Array.Empty<string>()),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,    "Yetkisiz erisim",  Array.Empty<string>()),
            _                       => (HttpStatusCode.InternalServerError, exception.Message,  Array.Empty<string>()),
        };

        context.Response.StatusCode = (int)statusCode;

        // Gelistirme ortaminda gercek hata detayini gonder
        var errorList = errors.Length > 0
            ? errors
            : (isDev ? [exception.Message] : Array.Empty<string>());

        var problem = new
        {
            type    = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status  = (int)statusCode,
            errors  = errorList,
            detail  = isDev ? exception.ToString() : null,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));
    }
}
