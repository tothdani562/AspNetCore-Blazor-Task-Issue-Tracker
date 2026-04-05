using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Services.Api;

namespace TaskTracker.Web.Services.Auth;

public class AuthApiClient : IAuthApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionStore _sessionStore;
    private readonly NavigationManager _navigationManager;

    public AuthApiClient(
        IHttpClientFactory httpClientFactory,
        IAuthSessionStore sessionStore,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStore = sessionStore;
        _navigationManager = navigationManager;
    }

    public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendAuthPayloadAsync("auth/register", request, cancellationToken);
    }

    public Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendAuthPayloadAsync("auth/login", request, cancellationToken);
    }

    public async Task<AuthUserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.GetAsync("auth/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorMapper.CreateExceptionAsync(response, cancellationToken);
        }

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthUserDto>>(cancellationToken);
        if (body?.Data is null)
        {
            throw new InvalidOperationException("Sikertelen valasz a /auth/me endpointtol.");
        }

        return body.Data;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsync("auth/logout", null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorMapper.CreateExceptionAsync(response, cancellationToken);
        }
    }

    private async Task<AuthResponseDto> SendAuthPayloadAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorMapper.CreateExceptionAsync(response, cancellationToken);
        }

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(cancellationToken);
        if (body?.Data is null)
        {
            throw new InvalidOperationException("Sikertelen valasz az auth endpointtol.");
        }

        return body.Data;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("TaskTrackerApi");
        client.BaseAddress ??= new Uri(_navigationManager.BaseUri);
        client.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrWhiteSpace(_sessionStore.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _sessionStore.AccessToken);
        }

        return client;
    }
}