using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Projects;
using TaskTracker.Web.Services.Projects;

namespace TaskTracker.Web.Controllers;

[ApiController]
[Authorize]
[Route("projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> Create(
        [FromBody] CreateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.CreateProjectAsync(currentUserId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ProjectDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectListItemDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.GetProjectsAsync(currentUserId, cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<ProjectListItemDto>>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.GetProjectAsync(currentUserId, id, cancellationToken);

        return Ok(new ApiResponse<ProjectDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> Update(
        Guid id,
        [FromBody] UpdateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.UpdateProjectAsync(currentUserId, id, request, cancellationToken);

        return Ok(new ApiResponse<ProjectDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _projectService.DeleteProjectAsync(currentUserId, id, cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true
        });
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> AddMember(
        Guid id,
        [FromBody] AddProjectMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.AddMemberAsync(currentUserId, id, request, cancellationToken);

        return Ok(new ApiResponse<ProjectDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpDelete("{id:guid}/members/{memberUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> RemoveMember(
        Guid id,
        Guid memberUserId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _projectService.RemoveMemberAsync(currentUserId, id, memberUserId, cancellationToken);

        return Ok(new ApiResponse<ProjectDetailsDto>
        {
            Success = true,
            Data = result
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
