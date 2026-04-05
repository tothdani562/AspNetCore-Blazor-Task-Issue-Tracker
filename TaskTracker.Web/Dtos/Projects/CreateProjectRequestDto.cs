using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Projects;

public class CreateProjectRequestDto
{
    [Required]
    [NotWhiteSpace]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
