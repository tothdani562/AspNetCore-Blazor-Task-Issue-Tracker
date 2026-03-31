using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Data;

namespace TaskTracker.Web.Controllers;

/// <summary>
/// Health check és root endpoint
/// </summary>
[ApiController]
[Route("")]
public class HealthCheckController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthCheckController> _logger;

    public HealthCheckController(ApplicationDbContext dbContext, ILogger<HealthCheckController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

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

    /// <summary>
    /// Database connectivity test - valós adatbázis query
    /// </summary>
    [HttpGet("health/db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<ApiResponse<object>> DatabaseHealth()
    {
        try
        {
            _logger.LogInformation("Database health check iniciálása");

            // Valós adatbázis query - User tábla elérhetőségének tesztelése
            var userCount = _dbContext.Users.Count();
            
            _logger.LogInformation("Database health check sikeres. Felhasználók száma: {UserCount}", userCount);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    status = "database_connected",
                    timestamp = DateTime.UtcNow,
                    userCount = userCount,
                    version = "0.1.0"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check sikertelen");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<object>
            {
                Success = false,
                Data = new
                {
                    status = "database_unavailable",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                }
            });
        }
    }
}
