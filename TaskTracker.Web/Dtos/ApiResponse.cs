namespace TaskTracker.Web.Dtos;

/// <summary>
/// Egységes sikeres API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
}

/// <summary>
/// Egységes sikeres API response (üres payload-hoz)
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; } = true;
}

/// <summary>
/// Egységes hiba response
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
