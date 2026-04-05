using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Comments;

namespace TaskTracker.Web.Services.Comments;

public interface ICommentService
{
    Task<CommentDetailsDto> CreateCommentAsync(Guid currentUserId, Guid taskId, CreateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaginatedResultDto<CommentListItemDto>> GetCommentsAsync(Guid currentUserId, Guid taskId, GetCommentsQueryDto query, CancellationToken cancellationToken = default);
    Task<CommentDetailsDto> GetCommentAsync(Guid currentUserId, Guid taskId, Guid commentId, CancellationToken cancellationToken = default);
    Task<CommentDetailsDto> UpdateCommentAsync(Guid currentUserId, Guid taskId, Guid commentId, UpdateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid currentUserId, Guid taskId, Guid commentId, CancellationToken cancellationToken = default);
}
