using TaskTracker.Web.Dtos.Projects;

namespace TaskTracker.Web.Services.Projects;

public interface IProjectService
{
    Task<ProjectDetailsDto> CreateProjectAsync(Guid currentUserId, CreateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> GetProjectAsync(Guid currentUserId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> UpdateProjectAsync(Guid currentUserId, Guid projectId, UpdateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid currentUserId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> AddMemberAsync(Guid currentUserId, Guid projectId, AddProjectMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<ProjectDetailsDto> RemoveMemberAsync(Guid currentUserId, Guid projectId, Guid memberUserId, CancellationToken cancellationToken = default);
}
