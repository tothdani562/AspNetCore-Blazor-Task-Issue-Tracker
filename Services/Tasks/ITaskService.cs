using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Tasks;

namespace TaskTracker.Web.Services.Tasks;

public interface ITaskService
{
    Task<TaskDetailsDto> CreateTaskAsync(Guid currentUserId, Guid projectId, CreateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task<PaginatedResultDto<TaskListItemDto>> GetTasksAsync(Guid currentUserId, Guid projectId, GetTasksQueryDto query, CancellationToken cancellationToken = default);
    Task<TaskDetailsDto> GetTaskAsync(Guid currentUserId, Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskDetailsDto> UpdateTaskAsync(Guid currentUserId, Guid projectId, Guid taskId, UpdateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid currentUserId, Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
}
