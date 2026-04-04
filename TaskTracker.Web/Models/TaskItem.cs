namespace TaskTracker.Web.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.TODO;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.MEDIUM;
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
