namespace TaskTracker.Web.Services.Auth;

public interface IAuthTokenStorage
{
    Task<AuthTokens?> GetTokensAsync(CancellationToken cancellationToken = default);
    Task SetTokensAsync(AuthTokens tokens, CancellationToken cancellationToken = default);
    Task ClearTokensAsync(CancellationToken cancellationToken = default);
}