using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Services.Auth;

public class AuthFacade
{
    private readonly IAuthApiClient _authApiClient;
    private readonly IAuthSessionStore _sessionStore;
    private readonly IAuthTokenStorage _tokenStorage;
    private readonly AppAuthenticationStateProvider _authStateProvider;
    private bool _restoreAttempted;

    public AuthFacade(
        IAuthApiClient authApiClient,
        IAuthSessionStore sessionStore,
        IAuthTokenStorage tokenStorage,
        AppAuthenticationStateProvider authStateProvider)
    {
        _authApiClient = authApiClient;
        _sessionStore = sessionStore;
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _authApiClient.LoginAsync(request, cancellationToken);
        await SetAuthenticatedSessionAsync(response, cancellationToken);
    }

    public async Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _authApiClient.RegisterAsync(request, cancellationToken);
        await SetAuthenticatedSessionAsync(response, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sessionStore.IsAuthenticated)
            {
                await _authApiClient.LogoutAsync(cancellationToken);
            }
        }
        catch
        {
            // A szerver oldali logout hiba ne blokkolja a kliens session torleset.
        }

        await ClearSessionAsync(cancellationToken);
    }

    public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_restoreAttempted)
        {
            return _sessionStore.IsAuthenticated;
        }

        _restoreAttempted = true;

        var tokens = await _tokenStorage.GetTokensAsync(cancellationToken);
        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            await ClearSessionAsync(cancellationToken);
            return false;
        }

        _sessionStore.SetTokens(tokens.AccessToken, tokens.RefreshToken);

        try
        {
            var currentUser = await _authApiClient.GetMeAsync(cancellationToken);
            _authStateProvider.SetAuthenticated(currentUser);
            return true;
        }
        catch
        {
            await ClearSessionAsync(cancellationToken);
            return false;
        }
    }

    private async Task SetAuthenticatedSessionAsync(AuthResponseDto response, CancellationToken cancellationToken)
    {
        _sessionStore.SetSession(response);
        _authStateProvider.SetAuthenticated(response.User);

        await _tokenStorage.SetTokensAsync(new AuthTokens
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken
        }, cancellationToken);
    }

    private async Task ClearSessionAsync(CancellationToken cancellationToken)
    {
        _authStateProvider.SetLoggedOut();
        await _tokenStorage.ClearTokensAsync(cancellationToken);
    }
}