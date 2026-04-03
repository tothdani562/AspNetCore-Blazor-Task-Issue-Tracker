using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Dtos.Tasks;

public class UpdateTaskRequestDto
{
    [MinLength(3)]
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskItemStatus? Status { get; set; }

    public TaskItemPriority? Priority { get; set; }

    public Guid? AssignedUserId { get; set; }

    public DateTime? DueDate { get; set; }
}
