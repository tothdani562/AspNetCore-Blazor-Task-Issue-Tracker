using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Tasks;

namespace TaskTracker.Web.Services.Tasks;

public interface ITasksApiClient
{
    Task<PaginatedResultDto<TaskListItemDto>> GetTasksAsync(Guid projectId, GetTasksQueryDto query, CancellationToken cancellationToken = default);
    Task<TaskDetailsDto> GetTaskByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskDetailsDto> CreateTaskAsync(Guid projectId, CreateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task<TaskDetailsDto> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
}
