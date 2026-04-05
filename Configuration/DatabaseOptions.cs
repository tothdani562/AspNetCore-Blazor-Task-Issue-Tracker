using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool EnableRetryOnFailure { get; set; } = true;

    [Range(1, 20)]
    public int MaxRetryCount { get; set; } = 5;

    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; set; } = 10;

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool FailOnStartupError { get; set; } = true;
}
