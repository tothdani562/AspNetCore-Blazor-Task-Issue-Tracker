using TaskTracker.Web.Dtos.Projects;

namespace TaskTracker.Web.Services.Projects;

public interface IProjectsApiClient
{
    Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> CreateProjectAsync(CreateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> AddMemberAsync(Guid projectId, AddProjectMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> RemoveMemberAsync(Guid projectId, Guid memberUserId, CancellationToken cancellationToken = default);
}
