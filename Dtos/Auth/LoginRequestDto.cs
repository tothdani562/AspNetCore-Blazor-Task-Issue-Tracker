using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Auth;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [NotWhiteSpace]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [NotWhiteSpace]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
