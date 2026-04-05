using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Dtos.Projects;

public class ProjectListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AuthUserDto Owner { get; set; } = new();
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
