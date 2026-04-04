using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Dtos.Tasks;

public class GetTasksQueryDto : IValidatableObject
{
    public TaskItemStatus? Status { get; set; }

    public TaskItemPriority? Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTime? DueFrom { get; set; }

    public DateTime? DueTo { get; set; }

    public TaskSortBy? SortBy { get; set; }

    public TaskSortOrder? SortOrder { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueFrom.HasValue && DueTo.HasValue && DueFrom.Value > DueTo.Value)
        {
            yield return new ValidationResult(
                "A dueFrom nem lehet nagyobb, mint a dueTo.",
                new[] { nameof(DueFrom), nameof(DueTo) });
        }
    }
}

public enum TaskSortBy
{
    CreatedAt = 0,
    DueDate = 1,
    Priority = 2,
    Status = 3
}

public enum TaskSortOrder
{
    Asc = 0,
    Desc = 1
}
