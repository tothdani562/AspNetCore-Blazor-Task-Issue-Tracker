using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Configuration;

public class AppCorsOptions
{
    public const string SectionName = "Cors";

    public List<string> AllowedOrigins { get; set; } = new();

    public bool AllowCredentials { get; set; } = false;
}
