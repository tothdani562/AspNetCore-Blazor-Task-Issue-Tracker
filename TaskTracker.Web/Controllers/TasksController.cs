using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Tasks;
using TaskTracker.Web.Services.Tasks;

namespace TaskTracker.Web.Controllers;

[ApiController]
[Authorize]
[Route("projects/{projectId:guid}/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> Create(
        Guid projectId,
        [FromBody] CreateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _taskService.CreateTaskAsync(currentUserId, projectId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<TaskDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResultDto<TaskListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaginatedResultDto<TaskListItemDto>>>> GetAll(
        Guid projectId,
        [FromQuery] GetTasksQueryDto query,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _taskService.GetTasksAsync(currentUserId, projectId, query, cancellationToken);

        return Ok(new ApiResponse<PaginatedResultDto<TaskListItemDto>>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> GetById(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _taskService.GetTaskAsync(currentUserId, projectId, taskId, cancellationToken);

        return Ok(new ApiResponse<TaskDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPatch("{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> Update(
        Guid projectId,
        Guid taskId,
        [FromBody] UpdateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _taskService.UpdateTaskAsync(currentUserId, projectId, taskId, request, cancellationToken);

        return Ok(new ApiResponse<TaskDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _taskService.DeleteTaskAsync(currentUserId, projectId, taskId, cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Ervenytelen felhasznalo azonosito.");
        }

        return userId;
    }
}
