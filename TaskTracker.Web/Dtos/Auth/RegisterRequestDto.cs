using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Dtos.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
