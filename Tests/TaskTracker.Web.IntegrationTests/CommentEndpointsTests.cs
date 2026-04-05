using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Comments;
using Xunit;

namespace TaskTracker.Web.IntegrationTests;

public class CommentEndpointsTests : IClassFixture<CommentApiFactory>
{
    private readonly CommentApiFactory _factory;

    public CommentEndpointsTests(CommentApiFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    [Fact]
    public async Task Member_can_create_get_update_and_delete_own_comment()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.MemberId);

        var createResponse = await client.PostAsJsonAsync($"tasks/{_factory.TaskId}/comments", new CreateCommentRequestDto
        {
            Content = "  New comment from member  "
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CommentDetailsDto>>();
        Assert.True(created?.Success);
        Assert.NotNull(created?.Data);
        Assert.Equal("New comment from member", created!.Data!.Content);
        Assert.Equal(_factory.MemberId, created.Data.AuthorId);

        var getResponse = await client.GetAsync($"tasks/{_factory.TaskId}/comments/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CommentDetailsDto>>();
        Assert.True(fetched?.Success);
        Assert.Equal(created.Data.Id, fetched?.Data?.Id);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"tasks/{_factory.TaskId}/comments/{created.Data.Id}")
        {
            Content = JsonContent.Create(new UpdateCommentRequestDto
            {
                Content = "Edited by author"
            })
        };

        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updated = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CommentDetailsDto>>();
        Assert.True(updated?.Success);
        Assert.Equal("Edited by author", updated?.Data?.Content);

        var deleteResponse = await client.DeleteAsync($"tasks/{_factory.TaskId}/comments/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var deleted = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.True(deleted?.Success);

        var getAfterDeleteResponse = await client.GetAsync($"tasks/{_factory.TaskId}/comments/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Comment_list_supports_pagination()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.GetAsync($"tasks/{_factory.TaskId}/comments?page=1&limit=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<CommentListItemDto>>>();
        Assert.True(payload?.Success);
        Assert.NotNull(payload?.Data);
        Assert.Equal(1, payload!.Data!.Page);
        Assert.Equal(1, payload.Data.Limit);
        Assert.Equal(2, payload.Data.TotalCount);
        Assert.Equal(2, payload.Data.TotalPages);
        Assert.Single(payload.Data.Items);
        Assert.Equal(_factory.OwnerCommentId, payload.Data.Items[0].Id);
    }

    [Fact]
    public async Task Owner_can_update_and_delete_member_comment()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"tasks/{_factory.TaskId}/comments/{_factory.MemberCommentId}")
        {
            Content = JsonContent.Create(new UpdateCommentRequestDto
            {
                Content = "Owner updated this comment"
            })
        };

        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updated = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CommentDetailsDto>>();
        Assert.True(updated?.Success);
        Assert.Equal("Owner updated this comment", updated?.Data?.Content);

        var deleteResponse = await client.DeleteAsync($"tasks/{_factory.TaskId}/comments/{_factory.MemberCommentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Non_author_member_cannot_update_or_delete_comment()
    {
        var ownerClient = _factory.CreateAuthenticatedClient(_factory.OwnerId);
        var createResponse = await ownerClient.PostAsJsonAsync($"tasks/{_factory.TaskId}/comments", new CreateCommentRequestDto
        {
            Content = "Owner private note"
        });

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CommentDetailsDto>>();
        Assert.NotNull(created?.Data);

        var memberClient = _factory.CreateAuthenticatedClient(_factory.MemberId);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"tasks/{_factory.TaskId}/comments/{created!.Data!.Id}")
        {
            Content = JsonContent.Create(new UpdateCommentRequestDto
            {
                Content = "Unauthorized edit"
            })
        };

        var patchResponse = await memberClient.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);

        var deleteResponse = await memberClient.DeleteAsync($"tasks/{_factory.TaskId}/comments/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Outsider_cannot_access_task_comments()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OutsiderId);

        var response = await client.GetAsync($"tasks/{_factory.TaskId}/comments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_pagination_query_returns_bad_request()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.GetAsync($"tasks/{_factory.TaskId}/comments?page=0&limit=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
    }
}
