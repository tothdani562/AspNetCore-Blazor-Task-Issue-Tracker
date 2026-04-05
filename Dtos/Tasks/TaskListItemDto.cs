using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Dtos.Tasks;

public class TaskListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public AuthUserDto? AssignedUser { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
