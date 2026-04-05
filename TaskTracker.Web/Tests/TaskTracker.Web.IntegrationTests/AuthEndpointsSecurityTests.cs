using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Auth;
using Xunit;

namespace TaskTracker.Web.IntegrationTests;

public class AuthEndpointsSecurityTests
{
    [Fact]
    public async Task Me_without_token_returns_unified_401_error()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
        Assert.Equal(StatusCodes.Status401Unauthorized, error.StatusCode);
        Assert.Equal("/auth/me", error.Path);
    }

    [Fact]
    public async Task Register_rejects_whitespace_full_name_with_400()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("auth/register", new RegisterRequestDto
        {
            Email = "whitespace-user@example.com",
            FullName = "   ",
            Password = "StrongPass123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_whitespace_refresh_token_with_400()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("auth/refresh", new RefreshTokenRequestDto
        {
            RefreshToken = "   "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited_after_repeated_failed_attempts()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? rateLimitedResponse = null;

        for (var attempt = 1; attempt <= 20; attempt++)
        {
            var response = await client.PostAsJsonAsync("auth/login", new LoginRequestDto
            {
                Email = "unknown@example.com",
                Password = "SomePass123"
            });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        Assert.NotNull(rateLimitedResponse);

        var error = await rateLimitedResponse!.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
        Assert.Equal(StatusCodes.Status429TooManyRequests, error.StatusCode);
        Assert.Equal("/auth/login", error.Path);
    }
}
