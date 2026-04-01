using Microsoft.JSInterop;

namespace TaskTracker.Web.Services.Auth;

public class BrowserAuthTokenStorage : IAuthTokenStorage
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserAuthTokenStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<AuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<AuthTokens?>("taskTrackerAuth.getTokens");
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SetTokensAsync(AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("taskTrackerAuth.setTokens", tokens.AccessToken, tokens.RefreshToken);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public async Task ClearTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("taskTrackerAuth.clearTokens");
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}