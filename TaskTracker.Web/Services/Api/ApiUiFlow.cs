using Microsoft.AspNetCore.Components;

namespace TaskTracker.Web.Services.Api;

public static class ApiUiFlow
{
    public static bool TryHandleNavigation(Exception exception, NavigationManager navigationManager)
    {
        if (exception is not ApiClientException apiException)
        {
            return false;
        }

        var route = apiException.StatusCode switch
        {
            401 => "/401",
            403 => "/403",
            404 => "/404",
            _ => null
        };

        if (route is null)
        {
            return false;
        }

        navigationManager.NavigateTo(route, forceLoad: false);
        return true;
    }

    public static string ToUserMessage(Exception exception)
    {
        return exception is ApiClientException apiException
            ? apiException.Message
            : exception.Message;
    }
}
