namespace TaskTracker.Web.Services.Api;

public sealed class ApiClientException : Exception
{
    public ApiClientException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
