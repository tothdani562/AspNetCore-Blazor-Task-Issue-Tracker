using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    [NotWhiteSpace]
    public string RefreshToken { get; set; } = string.Empty;
}
