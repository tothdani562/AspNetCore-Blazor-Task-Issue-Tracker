using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Tasks;
using TaskTracker.Web.Models;
using Xunit;

namespace TaskTracker.Web.IntegrationTests;

public class TaskEndpointsTests : IClassFixture<TaskApiFactory>
{
    private readonly TaskApiFactory _factory;

    public TaskEndpointsTests(TaskApiFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    [Fact]
    public async Task Member_can_create_get_update_and_delete_task()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.MemberId);

        var createResponse = await client.PostAsJsonAsync($"projects/{_factory.ProjectId}/tasks", new CreateTaskRequestDto
        {
            Title = "Fix flaky integration test",
            Description = "Investigate random timeout",
            Status = TaskItemStatus.TODO,
            Priority = TaskItemPriority.HIGH,
            AssignedUserId = _factory.OwnerId,
            DueDate = DateTime.UtcNow.AddDays(2)
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDetailsDto>>();
        Assert.True(created?.Success);
        Assert.NotNull(created?.Data);
        Assert.Equal("Fix flaky integration test", created!.Data!.Title);

        var getResponse = await client.GetAsync($"projects/{_factory.ProjectId}/tasks/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDetailsDto>>();
        Assert.True(fetched?.Success);
        Assert.Equal(created.Data.Id, fetched?.Data?.Id);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"projects/{_factory.ProjectId}/tasks/{created.Data.Id}")
        {
            Content = JsonContent.Create(new UpdateTaskRequestDto
            {
                Status = TaskItemStatus.IN_PROGRESS,
                Priority = TaskItemPriority.MEDIUM
            })
        };

        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updated = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDetailsDto>>();
        Assert.True(updated?.Success);
        Assert.Equal(TaskItemStatus.IN_PROGRESS, updated?.Data?.Status);
        Assert.Equal(TaskItemPriority.MEDIUM, updated?.Data?.Priority);

        var deleteResponse = await client.DeleteAsync($"projects/{_factory.ProjectId}/tasks/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var deleted = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.True(deleted?.Success);

        var getAfterDeleteResponse = await client.GetAsync($"projects/{_factory.ProjectId}/tasks/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Get_tasks_supports_filter_and_pagination()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.GetAsync(
            $"projects/{_factory.ProjectId}/tasks?status=TODO&priority=MEDIUM&assigneeId={_factory.OwnerId}&page=1&limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<TaskListItemDto>>>();
        Assert.True(payload?.Success);
        Assert.NotNull(payload?.Data);
        Assert.Equal(1, payload!.Data!.Page);
        Assert.Equal(1, payload.Data.Limit);
        Assert.Equal(1, payload.Data.TotalCount);
        Assert.Equal(1, payload.Data.TotalPages);
        Assert.Single(payload.Data.Items);
        Assert.Equal(_factory.TaskTwoId, payload.Data.Items[0].Id);
    }

    [Fact]
    public async Task Get_tasks_supports_due_date_range_and_sorting()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var dueFrom = Uri.EscapeDataString(DateTime.UtcNow.AddDays(2).ToString("O"));
        var dueTo = Uri.EscapeDataString(DateTime.UtcNow.AddDays(4).ToString("O"));

        var response = await client.GetAsync(
            $"projects/{_factory.ProjectId}/tasks?dueFrom={dueFrom}&dueTo={dueTo}&sortBy=dueDate&sortOrder=asc&page=1&limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<TaskListItemDto>>>();
        Assert.True(payload?.Success);
        Assert.NotNull(payload?.Data);
        Assert.Single(payload!.Data!.Items);
        Assert.Equal(_factory.TaskOneId, payload.Data.Items[0].Id);
    }

    [Fact]
    public async Task Get_tasks_supports_combined_filters_and_created_at_sorting()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var dueFrom = Uri.EscapeDataString(DateTime.UtcNow.AddDays(6).ToString("O"));
        var dueTo = Uri.EscapeDataString(DateTime.UtcNow.AddDays(8).ToString("O"));

        var response = await client.GetAsync(
            $"projects/{_factory.ProjectId}/tasks?status=TODO&priority=MEDIUM&assigneeId={_factory.OwnerId}&dueFrom={dueFrom}&dueTo={dueTo}&sortBy=createdAt&sortOrder=desc&page=1&limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<TaskListItemDto>>>();
        Assert.True(payload?.Success);
        Assert.NotNull(payload?.Data);
        Assert.Single(payload!.Data!.Items);
        Assert.Equal(_factory.TaskTwoId, payload.Data.Items[0].Id);
    }

    [Fact]
    public async Task Invalid_advanced_query_returns_bad_request()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.GetAsync($"projects/{_factory.ProjectId}/tasks?sortBy=invalidField&sortOrder=desc");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
    }

    [Fact]
    public async Task Outsider_cannot_access_project_tasks()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OutsiderId);

        var response = await client.GetAsync($"projects/{_factory.ProjectId}/tasks");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_pagination_query_returns_bad_request()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.GetAsync($"projects/{_factory.ProjectId}/tasks?page=0&limit=150");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
    }

    [Fact]
    public async Task Creating_task_with_unknown_assignee_returns_not_found()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var response = await client.PostAsJsonAsync($"projects/{_factory.ProjectId}/tasks", new CreateTaskRequestDto
        {
            Title = "Invalid assignment",
            Status = TaskItemStatus.TODO,
            Priority = TaskItemPriority.LOW,
            AssignedUserId = Guid.Parse("12345678-1234-1234-1234-123456789012")
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error!.Success);
    }
}
