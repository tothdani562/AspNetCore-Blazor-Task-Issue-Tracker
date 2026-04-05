using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Models;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Tasks;

public class CreateTaskRequestDto
{
    [Required]
    [NotWhiteSpace]
    [MinLength(3)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public TaskItemStatus Status { get; set; } = TaskItemStatus.TODO;

    [Required]
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.MEDIUM;

    public Guid? AssignedUserId { get; set; }

    public DateTime? DueDate { get; set; }
}
