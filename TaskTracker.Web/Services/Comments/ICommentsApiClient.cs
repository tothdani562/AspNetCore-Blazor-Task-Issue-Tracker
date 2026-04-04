using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Comments;

namespace TaskTracker.Web.Services.Comments;

public interface ICommentsApiClient
{
    Task<PaginatedResultDto<CommentListItemDto>> GetCommentsAsync(Guid taskId, GetCommentsQueryDto query, CancellationToken cancellationToken = default);
    Task<CommentDetailsDto> CreateCommentAsync(Guid taskId, CreateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<CommentDetailsDto> UpdateCommentAsync(Guid taskId, Guid commentId, UpdateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken = default);
}
