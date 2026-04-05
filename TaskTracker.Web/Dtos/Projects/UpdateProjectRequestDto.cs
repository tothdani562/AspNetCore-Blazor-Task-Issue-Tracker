using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Projects;

public class UpdateProjectRequestDto
{
    [NotWhiteSpace]
    [MinLength(3)]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
