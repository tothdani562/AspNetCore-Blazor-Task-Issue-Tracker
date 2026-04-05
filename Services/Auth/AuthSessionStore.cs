using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Services.Auth;

public class AuthSessionStore : IAuthSessionStore
{
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public AuthUserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        CurrentUser is not null;

    public void SetSession(AuthResponseDto response)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        CurrentUser = response.User;
    }

    public void SetTokens(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public void SetUser(AuthUserDto user)
    {
        CurrentUser = user;
    }

    public void Clear()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        CurrentUser = null;
    }
}