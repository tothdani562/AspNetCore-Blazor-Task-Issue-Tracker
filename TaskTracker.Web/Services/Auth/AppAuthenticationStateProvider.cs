using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TaskTracker.Web.Dtos.Auth;

namespace TaskTracker.Web.Services.Auth;

public class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthSessionStore _sessionStore;

    public AppAuthenticationStateProvider(IAuthSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_sessionStore.IsAuthenticated || _sessionStore.CurrentUser is null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var user = _sessionStore.CurrentUser;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, "TaskTrackerJwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void SetAuthenticated(AuthUserDto user)
    {
        _sessionStore.SetUser(user);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SetLoggedOut()
    {
        _sessionStore.Clear();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}