using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Api.Filters;

/// <summary>
/// Action filter: her yetkili istekte lisans gecerliligi kontrol edilir.
/// [LicenseCheck] attribute ile controller veya action'a uygulanir.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LicenseCheckAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var tenantId = ctx.HttpContext.Items["TenantId"] as Guid?;

        if (tenantId is null || tenantId == Guid.Empty)
        {
            await next();
            return;
        }

        var licensingService = ctx.HttpContext.RequestServices
            .GetRequiredService<ILicensingService>();

        var check = await licensingService.CheckLicenseAsync(tenantId.Value);

        if (!check.IsValid)
        {
            ctx.Result = new ObjectResult(ApiResponse<object>.Fail(
                check.ErrorMessage ?? "Lisans gecersiz veya suresi dolmus."))
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return;
        }

        // Lisans bilgisini context'e ekle (controller'larda kullanilabilir)
        ctx.HttpContext.Items["LicenseTier"]    = check.Tier;
        ctx.HttpContext.Items["LicenseIsValid"] = true;

        await next();
    }
}
