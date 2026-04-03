namespace TaskTracker.Web.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
