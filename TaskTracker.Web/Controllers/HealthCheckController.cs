using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Dtos;

namespace TaskTracker.Web.Controllers;

/// <summary>
/// Health check és root endpoint
/// </summary>
[ApiController]
[Route("")]
public class HealthCheckController : ControllerBase
{
    /// <summary>
    /// Root endpoint - health-check jellegű válasz
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> HealthCheck()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "0.1.0"
            }
        });
    }
}
