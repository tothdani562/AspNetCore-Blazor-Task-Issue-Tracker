using Microsoft.EntityFrameworkCore;
using TaskTracker.Web.Data;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Dtos.Comments;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services.Comments;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _dbContext;

    public CommentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommentDetailsDto> CreateCommentAsync(
        Guid currentUserId,
        Guid taskId,
        CreateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskForAccessAsync(taskId, cancellationToken);
        var project = GetProjectOrThrow(task);
        EnsureProjectAccess(project, currentUserId);

        var now = DateTime.UtcNow;

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            AuthorId = currentUserId,
            Content = NormalizeContent(request.Content),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCommentAsync(currentUserId, taskId, comment.Id, cancellationToken);
    }

    public async Task<PaginatedResultDto<CommentListItemDto>> GetCommentsAsync(
        Guid currentUserId,
        Guid taskId,
        GetCommentsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskForAccessAsync(taskId, cancellationToken);
        var project = GetProjectOrThrow(task);
        EnsureProjectAccess(project, currentUserId);

        var commentsQuery = _dbContext.Comments
            .AsNoTracking()
            .Include(comment => comment.Author)
            .Where(comment => comment.TaskId == taskId)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id);

        var totalCount = await commentsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.Limit);

        var items = await commentsQuery
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return new PaginatedResultDto<CommentListItemDto>
        {
            Items = items.Select(MapToListItem).ToList(),
            Page = query.Page,
            Limit = query.Limit,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<CommentDetailsDto> GetCommentAsync(
        Guid currentUserId,
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskForAccessAsync(taskId, cancellationToken);
        var project = GetProjectOrThrow(task);
        EnsureProjectAccess(project, currentUserId);

        var comment = await LoadCommentAsync(taskId, commentId, tracking: false, cancellationToken);
        return MapToDetails(comment);
    }

    public async Task<CommentDetailsDto> UpdateCommentAsync(
        Guid currentUserId,
        Guid taskId,
        Guid commentId,
        UpdateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskForAccessAsync(taskId, cancellationToken);
        var project = GetProjectOrThrow(task);
        EnsureProjectAccess(project, currentUserId);

        var comment = await LoadCommentAsync(taskId, commentId, tracking: true, cancellationToken);
        EnsureAuthorOrProjectOwner(project, comment, currentUserId);

        comment.Content = NormalizeContent(request.Content);
        comment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCommentAsync(currentUserId, taskId, commentId, cancellationToken);
    }

    public async Task DeleteCommentAsync(
        Guid currentUserId,
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskForAccessAsync(taskId, cancellationToken);
        var project = GetProjectOrThrow(task);
        EnsureProjectAccess(project, currentUserId);

        var comment = await LoadCommentAsync(taskId, commentId, tracking: true, cancellationToken);
        EnsureAuthorOrProjectOwner(project, comment, currentUserId);

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> LoadTaskForAccessAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .ThenInclude(project => project!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("A megadott task nem talalhato.");
        }

        return task;
    }

    private async Task<Comment> LoadCommentAsync(Guid taskId, Guid commentId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Comment> query = _dbContext.Comments;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var comment = await query
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.TaskId == taskId && c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException("A megadott komment nem talalhato.");
        }

        return comment;
    }

    private static void EnsureProjectAccess(Project project, Guid currentUserId)
    {
        if (project.OwnerId == currentUserId)
        {
            return;
        }

        if (project.Members.Any(member => member.UserId == currentUserId))
        {
            return;
        }

        throw new ForbiddenException("Nincs jogosultsagod a task kommentjeihez.");
    }

    private static void EnsureAuthorOrProjectOwner(Project project, Comment comment, Guid currentUserId)
    {
        if (project.OwnerId == currentUserId || comment.AuthorId == currentUserId)
        {
            return;
        }

        throw new ForbiddenException("Csak a komment szerzoje vagy a projekt tulajdonosa modosithat vagy torolhet kommentet.");
    }

    private static CommentListItemDto MapToListItem(Comment comment)
    {
        return new CommentListItemDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            Author = new AuthUserDto
            {
                Id = comment.Author.Id,
                Email = comment.Author.Email,
                FullName = comment.Author.FullName
            },
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    private static CommentDetailsDto MapToDetails(Comment comment)
    {
        var item = MapToListItem(comment);

        return new CommentDetailsDto
        {
            Id = item.Id,
            TaskId = item.TaskId,
            AuthorId = item.AuthorId,
            Author = item.Author,
            Content = item.Content,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static string NormalizeContent(string content)
    {
        var normalized = content?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A komment tartalma nem lehet ures.");
        }

        return normalized;
    }

    private static Project GetProjectOrThrow(TaskItem task)
    {
        return task.Project ?? throw new NotFoundException("A taskhoz tartozo projekt nem talalhato.");
    }
}
