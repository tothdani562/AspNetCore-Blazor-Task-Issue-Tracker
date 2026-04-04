using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Tasks;
using TaskTracker.Web.Services.Auth;

namespace TaskTracker.Web.Services.Tasks;

public class TasksApiClient : ITasksApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionStore _sessionStore;
    private readonly NavigationManager _navigationManager;

    public TasksApiClient(
        IHttpClientFactory httpClientFactory,
        IAuthSessionStore sessionStore,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStore = sessionStore;
        _navigationManager = navigationManager;
    }

    public async Task<PaginatedResultDto<TaskListItemDto>> GetTasksAsync(Guid projectId, GetTasksQueryDto query, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var queryString = BuildQueryString(query);
        using var response = await client.GetAsync($"projects/{projectId}/tasks{queryString}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<TaskListItemDto>>>(cancellationToken);
        return body?.Data ?? new PaginatedResultDto<TaskListItemDto>();
    }

    public async Task<TaskDetailsDto> GetTaskByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.GetAsync($"projects/{projectId}/tasks/{taskId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<TaskDetailsDto>(response, cancellationToken, "Sikertelen valasz a feladat lekerdezesenel.");
    }

    public async Task<TaskDetailsDto> CreateTaskAsync(Guid projectId, CreateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync($"projects/{projectId}/tasks", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<TaskDetailsDto>(response, cancellationToken, "Sikertelen valasz a feladat letrehozasakor.");
    }

    public async Task<TaskDetailsDto> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PatchAsJsonAsync($"projects/{projectId}/tasks/{taskId}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<TaskDetailsDto>(response, cancellationToken, "Sikertelen valasz a feladat frissitesekor.");
    }

    public async Task DeleteTaskAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.DeleteAsync($"projects/{projectId}/tasks/{taskId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
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
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
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

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch
        {
        }

        return $"A keres nem sikerult. HTTP {(int)response.StatusCode}.";
    }

    private static string BuildQueryString(GetTasksQueryDto query)
    {
        var parameters = new List<string>();

        if (query.Status.HasValue)
            parameters.Add($"status={(int)query.Status.Value}");

        if (query.Priority.HasValue)
            parameters.Add($"priority={(int)query.Priority.Value}");

        if (query.AssigneeId.HasValue)
            parameters.Add($"assigneeId={query.AssigneeId.Value}");

        if (query.DueFrom.HasValue)
            parameters.Add($"dueFrom={query.DueFrom.Value:o}");

        if (query.DueTo.HasValue)
            parameters.Add($"dueTo={query.DueTo.Value:o}");

        if (query.SortBy.HasValue)
            parameters.Add($"sortBy={query.SortBy.Value}");

        if (query.SortOrder.HasValue)
            parameters.Add($"sortOrder={query.SortOrder.Value}");

        if (query.Page > 0)
            parameters.Add($"page={query.Page}");

        if (query.Limit > 0)
            parameters.Add($"limit={query.Limit}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;
    }
}

