using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Dtos.Projects;

public class CreateProjectRequestDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
