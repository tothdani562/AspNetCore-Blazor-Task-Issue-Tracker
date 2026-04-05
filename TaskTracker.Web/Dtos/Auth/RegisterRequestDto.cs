using System.ComponentModel.DataAnnotations;
using TaskTracker.Web.Validation;

namespace TaskTracker.Web.Dtos.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [NotWhiteSpace]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [NotWhiteSpace]
    [MinLength(2)]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [NotWhiteSpace]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,100}$", ErrorMessage = "A jelszonak tartalmaznia kell kis- es nagybetut, valamint szamot.")]
    public string Password { get; set; } = string.Empty;
}
