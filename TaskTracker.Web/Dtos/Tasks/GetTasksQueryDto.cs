using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Dtos.Tasks;

public class GetTasksQueryDto
{
    public TaskItemStatus? Status { get; set; }

    public TaskItemPriority? Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
