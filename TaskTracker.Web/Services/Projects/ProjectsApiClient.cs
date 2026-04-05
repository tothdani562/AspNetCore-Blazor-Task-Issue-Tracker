using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Projects;
using TaskTracker.Web.Services.Api;
using TaskTracker.Web.Services.Auth;

namespace TaskTracker.Web.Services.Projects;

public class ProjectsApiClient : IProjectsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionStore _sessionStore;
    private readonly NavigationManager _navigationManager;

    public ProjectsApiClient(
        IHttpClientFactory httpClientFactory,
        IAuthSessionStore sessionStore,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStore = sessionStore;
        _navigationManager = navigationManager;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.GetAsync("projects", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProjectListItemDto>>>(cancellationToken);
        return body?.Data ?? new List<ProjectListItemDto>();
    }

    public async Task<ProjectDetailsDto> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.GetAsync($"projects/{projectId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<ProjectDetailsDto>(response, cancellationToken, "Sikertelen valasz a projekt lekerdezesenel.");
    }

    public async Task<ProjectDetailsDto> CreateProjectAsync(CreateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("projects", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<ProjectDetailsDto>(response, cancellationToken, "Sikertelen valasz a projekt letrehozasakor.");
    }

    public async Task<ProjectDetailsDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PatchAsJsonAsync($"projects/{projectId}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<ProjectDetailsDto>(response, cancellationToken, "Sikertelen valasz a projekt frissitesekor.");
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.DeleteAsync($"projects/{projectId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<ProjectDetailsDto> AddMemberAsync(Guid projectId, AddProjectMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync($"projects/{projectId}/members", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<ProjectDetailsDto>(response, cancellationToken, "Sikertelen valasz a tag hozzaadasakor.");
    }

    public async Task<ProjectDetailsDto> RemoveMemberAsync(Guid projectId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.DeleteAsync($"projects/{projectId}/members/{memberUserId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<ProjectDetailsDto>(response, cancellationToken, "Sikertelen valasz a tag eltavolitasakor.");
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

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiErrorMapper.CreateExceptionAsync(response, cancellationToken);
        }
    }

    private static async Task<T> ReadDataAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        string fallbackMessage)
        where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
        var data = body?.Data;
        if (data is null)
        {
            throw new InvalidOperationException(fallbackMessage);
        }

        return data;
    }
}
