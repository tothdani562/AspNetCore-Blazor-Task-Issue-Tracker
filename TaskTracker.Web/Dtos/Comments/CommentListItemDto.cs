using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Dtos.Comments;

public class CommentListItemDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public AuthUserDto Author { get; set; } = new();
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
