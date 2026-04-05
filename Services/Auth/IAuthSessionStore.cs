using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Services.Auth;

public interface IAuthSessionStore
{
    string AccessToken { get; }
    string RefreshToken { get; }
    AuthUserDto? CurrentUser { get; }
    bool IsAuthenticated { get; }

    void SetSession(AuthResponseDto response);
    void SetTokens(string accessToken, string refreshToken);
    void SetUser(AuthUserDto user);
    void Clear();
}