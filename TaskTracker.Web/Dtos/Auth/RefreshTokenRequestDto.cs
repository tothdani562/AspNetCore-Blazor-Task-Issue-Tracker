using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Dtos.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
