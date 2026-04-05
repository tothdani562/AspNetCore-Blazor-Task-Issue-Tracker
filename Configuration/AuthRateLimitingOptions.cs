using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Configuration;

public class AuthRateLimitingOptions
{
    public const string SectionName = "RateLimiting:Auth";

    [Range(1, 1000)]
    public int PermitLimit { get; set; } = 8;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
