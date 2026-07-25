using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Models;

namespace Senkora.Api.Controllers.v1;

/// <summary>Sistem saglik kontrolu</summary>
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            status    = "Healthy",
            version   = "1.0.0",
            timestamp = DateTime.UtcNow
        }));
    }
}
