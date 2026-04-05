using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Dtos;

namespace TaskTracker.Web.Services.Api;

public static class ApiErrorMapper
{
    public static async Task<ApiClientException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? message = null;

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                message = error.Message.Trim();
            }
        }
        catch
        {
            // Ignore parse issues and use fallback text below.
        }

        message ??= GetFallbackMessage(response.StatusCode);
        return new ApiClientException((int)response.StatusCode, message);
    }

    private static string GetFallbackMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Hibas keres.",
            HttpStatusCode.Unauthorized => "A munkamenet lejart vagy ervenytelen. Jelentkezz be ujra.",
            HttpStatusCode.Forbidden => "Nincs jogosultsagod ehhez a muvelethez.",
            HttpStatusCode.NotFound => "A kert eroforras nem talalhato.",
            HttpStatusCode.Conflict => "Utkzes tortent a muvelet kozben.",
            _ => $"A keres nem sikerult. HTTP {(int)statusCode}."
        };
    }
}
