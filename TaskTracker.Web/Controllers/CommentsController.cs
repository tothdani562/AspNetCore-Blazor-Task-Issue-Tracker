using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Comments;
using TaskTracker.Web.Services.Comments;

namespace TaskTracker.Web.Controllers;

[ApiController]
[Authorize]
[Route("tasks/{taskId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CommentDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CommentDetailsDto>>> Create(
        Guid taskId,
        [FromBody] CreateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _commentService.CreateCommentAsync(currentUserId, taskId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<CommentDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResultDto<CommentListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaginatedResultDto<CommentListItemDto>>>> GetAll(
        Guid taskId,
        [FromQuery] GetCommentsQueryDto query,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _commentService.GetCommentsAsync(currentUserId, taskId, query, cancellationToken);

        return Ok(new ApiResponse<PaginatedResultDto<CommentListItemDto>>
        {
            Success = true,
            Data = result
        });
    }

    [HttpGet("{commentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CommentDetailsDto>>> GetById(
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _commentService.GetCommentAsync(currentUserId, taskId, commentId, cancellationToken);

        return Ok(new ApiResponse<CommentDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPatch("{commentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CommentDetailsDto>>> Update(
        Guid taskId,
        Guid commentId,
        [FromBody] UpdateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _commentService.UpdateCommentAsync(currentUserId, taskId, commentId, request, cancellationToken);

        return Ok(new ApiResponse<CommentDetailsDto>
        {
            Success = true,
            Data = result
        });
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _commentService.DeleteCommentAsync(currentUserId, taskId, commentId, cancellationToken);

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
