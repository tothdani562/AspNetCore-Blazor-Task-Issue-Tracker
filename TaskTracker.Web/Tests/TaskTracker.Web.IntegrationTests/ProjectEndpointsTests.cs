using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Projects;
using Xunit;

namespace TaskTracker.Web.IntegrationTests;

public class ProjectEndpointsTests : IClassFixture<ProjectApiFactory>
{
    private readonly ProjectApiFactory _factory;

    public ProjectEndpointsTests(ProjectApiFactory factory)
    {
        _factory = factory;
        _factory.Seed();
    }

    [Fact]
    public async Task Owner_can_create_and_list_projects()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var createResponse = await client.PostAsJsonAsync("projects", new CreateProjectRequestDto
        {
            Name = "New Project",
            Description = "Created during integration test"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDetailsDto>>();
        Assert.True(created?.Success);
        Assert.NotNull(created?.Data);
        Assert.Equal("New Project", created!.Data!.Name);
        Assert.Equal(_factory.OwnerId, created.Data.Owner.Id);

        var listResponse = await client.GetAsync("projects");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<ProjectListItemDto>>>();
        Assert.True(list?.Success);
        Assert.NotNull(list?.Data);
        Assert.Contains(list!.Data!, project => project.Id == _factory.ProjectId);
        Assert.Contains(list.Data, project => project.Name == "New Project");
    }

    [Fact]
    public async Task Member_can_read_project_but_cannot_modify_it()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.MemberId);

        var getResponse = await client.GetAsync($"projects/{_factory.ProjectId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var details = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDetailsDto>>();
        Assert.True(details?.Success);
        Assert.Equal(_factory.ProjectId, details?.Data?.Id);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"projects/{_factory.ProjectId}")
        {
            Content = JsonContent.Create(new UpdateProjectRequestDto
            {
                Name = "Blocked Update"
            })
        };

        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
    }

    [Fact]
    public async Task Owner_can_add_and_remove_members()
    {
        var client = _factory.CreateAuthenticatedClient(_factory.OwnerId);

        var addResponse = await client.PostAsJsonAsync($"projects/{_factory.ProjectId}/members", new AddProjectMemberRequestDto
        {
            MemberUserId = _factory.OutsiderId
        });

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var added = await addResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDetailsDto>>();
        Assert.True(added?.Success);
        Assert.Contains(added!.Data!.Members, member => member.UserId == _factory.OutsiderId);

        var removeResponse = await client.DeleteAsync($"projects/{_factory.ProjectId}/members/{_factory.OutsiderId}");
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

        var removed = await removeResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDetailsDto>>();
        Assert.True(removed?.Success);
        Assert.DoesNotContain(removed!.Data!.Members, member => member.UserId == _factory.OutsiderId);
    }
}
