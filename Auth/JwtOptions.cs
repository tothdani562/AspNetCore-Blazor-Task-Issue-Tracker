using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(3)]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 30)]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
