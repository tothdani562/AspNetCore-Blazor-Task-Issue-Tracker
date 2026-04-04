using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Comments;
using TaskTracker.Web.Services.Auth;

namespace TaskTracker.Web.Services.Comments;

public class CommentsApiClient : ICommentsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionStore _sessionStore;
    private readonly NavigationManager _navigationManager;

    public CommentsApiClient(
        IHttpClientFactory httpClientFactory,
        IAuthSessionStore sessionStore,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStore = sessionStore;
        _navigationManager = navigationManager;
    }

    public async Task<PaginatedResultDto<CommentListItemDto>> GetCommentsAsync(Guid taskId, GetCommentsQueryDto query, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var queryString = BuildQueryString(query);
        using var response = await client.GetAsync($"tasks/{taskId}/comments{queryString}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<CommentListItemDto>>>(cancellationToken);
        return body?.Data ?? new PaginatedResultDto<CommentListItemDto>();
    }

    public async Task<CommentDetailsDto> CreateCommentAsync(Guid taskId, CreateCommentRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync($"tasks/{taskId}/comments", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<CommentDetailsDto>(response, cancellationToken, "Sikertelen valasz a komment letrehozasakor.");
    }

    public async Task<CommentDetailsDto> UpdateCommentAsync(Guid taskId, Guid commentId, UpdateCommentRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PatchAsJsonAsync($"tasks/{taskId}/comments/{commentId}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadDataAsync<CommentDetailsDto>(response, cancellationToken, "Sikertelen valasz a komment frissitesekor.");
    }

    public async Task DeleteCommentAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.DeleteAsync($"tasks/{taskId}/comments/{commentId}", cancellationToken);
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

    private static string BuildQueryString(GetCommentsQueryDto query)
    {
        var parameters = new List<string>();

        if (query.Page > 0)
        {
            parameters.Add($"page={query.Page}");
        }

        if (query.Limit > 0)
        {
            parameters.Add($"limit={query.Limit}");
        }

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;
    }
}
