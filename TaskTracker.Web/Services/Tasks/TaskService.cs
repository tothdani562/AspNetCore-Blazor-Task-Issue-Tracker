using Microsoft.EntityFrameworkCore;
using TaskTracker.Web.Data;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Dtos.Tasks;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services.Tasks;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _dbContext;

    public TaskService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskDetailsDto> CreateTaskAsync(
        Guid currentUserId,
        Guid projectId,
        CreateTaskRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForAccessAsync(projectId, cancellationToken);
        EnsureProjectAccess(project, currentUserId);

        if (request.AssignedUserId.HasValue)
        {
            await EnsureAssignableUserAsync(project, request.AssignedUserId.Value, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var dueDate = request.DueDate.HasValue
            ? (DateTime?)DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            AssignedUserId = request.AssignedUserId,
            DueDate = dueDate,
            ProjectId = projectId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTaskAsync(currentUserId, projectId, task.Id, cancellationToken);
    }

    public async Task<PaginatedResultDto<TaskListItemDto>> GetTasksAsync(
        Guid currentUserId,
        Guid projectId,
        GetTasksQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForAccessAsync(projectId, cancellationToken);
        EnsureProjectAccess(project, currentUserId);

        var tasksQuery = _dbContext.Tasks
            .AsNoTracking()
            .Include(task => task.AssignedUser)
            .Where(task => task.ProjectId == projectId);

        if (query.Status.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.Priority == query.Priority.Value);
        }

        if (query.AssigneeId.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.AssignedUserId == query.AssigneeId.Value);
        }

        if (query.DueFrom.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.DueDate.HasValue && task.DueDate.Value >= query.DueFrom.Value);
        }

        if (query.DueTo.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.DueDate.HasValue && task.DueDate.Value <= query.DueTo.Value);
        }

        tasksQuery = ApplySorting(tasksQuery, query);

        var totalCount = await tasksQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.Limit);

        var items = await tasksQuery
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return new PaginatedResultDto<TaskListItemDto>
        {
            Items = items.Select(MapToListItem).ToList(),
            Page = query.Page,
            Limit = query.Limit,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<TaskDetailsDto> GetTaskAsync(
        Guid currentUserId,
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await LoadTaskAsync(projectId, taskId, tracking: false, cancellationToken);
        var project = await LoadProjectForAccessAsync(projectId, cancellationToken);

        EnsureProjectAccess(project, currentUserId);

        return MapToDetails(task);
    }

    public async Task<TaskDetailsDto> UpdateTaskAsync(
        Guid currentUserId,
        Guid projectId,
        Guid taskId,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForAccessAsync(projectId, cancellationToken);
        EnsureProjectAccess(project, currentUserId);

        var task = await LoadTaskAsync(projectId, taskId, tracking: true, cancellationToken);

        var hasChange = false;

        if (request.Title is not null)
        {
            var trimmedTitle = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                throw new ArgumentException("A task cime nem lehet ures.");
            }

            task.Title = trimmedTitle;
            hasChange = true;
        }

        if (request.Description is not null)
        {
            task.Description = NormalizeDescription(request.Description);
            hasChange = true;
        }

        if (request.Status.HasValue)
        {
            task.Status = request.Status.Value;
            hasChange = true;
        }

        if (request.Priority.HasValue)
        {
            task.Priority = request.Priority.Value;
            hasChange = true;
        }

        if (request.AssignedUserId.HasValue)
        {
            await EnsureAssignableUserAsync(project, request.AssignedUserId.Value, cancellationToken);
            task.AssignedUserId = request.AssignedUserId.Value;
            hasChange = true;
        }

        if (request.DueDate.HasValue)
        {
            task.DueDate = DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc);
            hasChange = true;
        }

        if (!hasChange)
        {
            throw new ArgumentException("Legalabb egy mezot meg kell adni a modositashez.");
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTaskAsync(currentUserId, projectId, taskId, cancellationToken);
    }

    public async Task DeleteTaskAsync(
        Guid currentUserId,
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForAccessAsync(projectId, cancellationToken);
        EnsureProjectAccess(project, currentUserId);

        var task = await LoadTaskAsync(projectId, taskId, tracking: true, cancellationToken);

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> LoadProjectForAccessAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("A megadott projekt nem talalhato.");
        }

        return project;
    }

    private async Task<TaskItem> LoadTaskAsync(Guid projectId, Guid taskId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<TaskItem> query = _dbContext.Tasks;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        query = query.Include(task => task.AssignedUser);

        var task = await query.FirstOrDefaultAsync(task => task.ProjectId == projectId && task.Id == taskId, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException("A megadott task nem talalhato.");
        }

        return task;
    }

    private async Task EnsureAssignableUserAsync(Project project, Guid assignedUserId, CancellationToken cancellationToken)
    {
        if (assignedUserId == project.OwnerId || project.Members.Any(member => member.UserId == assignedUserId))
        {
            return;
        }

        var userExists = await _dbContext.Users.AnyAsync(user => user.Id == assignedUserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("A hozzarendelt felhasznalo nem talalhato.");
        }

        throw new ArgumentException("Csak a projekt tulajdonosa vagy tagja rendelheto taskhoz.");
    }

    private static void EnsureProjectAccess(Project project, Guid currentUserId)
    {
        if (project.OwnerId == currentUserId)
        {
            return;
        }

        if (project.Members.Any(member => member.UserId == currentUserId))
        {
            return;
        }

        throw new ForbiddenException("Nincs jogosultsagod a projekt taskjaihoz.");
    }

    private static TaskListItemDto MapToListItem(TaskItem task)
    {
        return new TaskListItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            AssignedUserId = task.AssignedUserId,
            AssignedUser = MapAssignedUser(task.AssignedUser),
            DueDate = task.DueDate,
            ProjectId = task.ProjectId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    private static TaskDetailsDto MapToDetails(TaskItem task)
    {
        var listItem = MapToListItem(task);

        return new TaskDetailsDto
        {
            Id = listItem.Id,
            Title = listItem.Title,
            Description = listItem.Description,
            Status = listItem.Status,
            Priority = listItem.Priority,
            AssignedUserId = listItem.AssignedUserId,
            AssignedUser = listItem.AssignedUser,
            DueDate = listItem.DueDate,
            ProjectId = listItem.ProjectId,
            CreatedAt = listItem.CreatedAt,
            UpdatedAt = listItem.UpdatedAt
        };
    }

    private static AuthUserDto? MapAssignedUser(User? user)
    {
        if (user is null)
        {
            return null;
        }

        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, GetTasksQueryDto filter)
    {
        if (!filter.SortBy.HasValue)
        {
            return query
                .OrderByDescending(task => task.UpdatedAt)
                .ThenByDescending(task => task.CreatedAt)
                .ThenByDescending(task => task.Id);
        }

        var descending = filter.SortOrder != TaskSortOrder.Asc;

        return filter.SortBy.Value switch
        {
            TaskSortBy.CreatedAt => descending
                ? query.OrderByDescending(task => task.CreatedAt).ThenByDescending(task => task.Id)
                : query.OrderBy(task => task.CreatedAt).ThenBy(task => task.Id),
            TaskSortBy.DueDate => descending
                ? query.OrderBy(task => task.DueDate == null)
                    .ThenByDescending(task => task.DueDate)
                    .ThenByDescending(task => task.CreatedAt)
                    .ThenByDescending(task => task.Id)
                : query.OrderBy(task => task.DueDate == null)
                    .ThenBy(task => task.DueDate)
                    .ThenBy(task => task.CreatedAt)
                    .ThenBy(task => task.Id),
            TaskSortBy.Priority => descending
                ? query.OrderByDescending(task => task.Priority == TaskItemPriority.LOW ? 0 : task.Priority == TaskItemPriority.MEDIUM ? 1 : 2)
                    .ThenByDescending(task => task.CreatedAt)
                    .ThenByDescending(task => task.Id)
                : query.OrderBy(task => task.Priority == TaskItemPriority.LOW ? 0 : task.Priority == TaskItemPriority.MEDIUM ? 1 : 2)
                    .ThenBy(task => task.CreatedAt)
                    .ThenBy(task => task.Id),
            TaskSortBy.Status => descending
                ? query.OrderByDescending(task => task.Status == TaskItemStatus.TODO ? 0 : task.Status == TaskItemStatus.IN_PROGRESS ? 1 : 2)
                    .ThenByDescending(task => task.CreatedAt)
                    .ThenByDescending(task => task.Id)
                : query.OrderBy(task => task.Status == TaskItemStatus.TODO ? 0 : task.Status == TaskItemStatus.IN_PROGRESS ? 1 : 2)
                    .ThenBy(task => task.CreatedAt)
                    .ThenBy(task => task.Id),
            _ => query.OrderByDescending(task => task.UpdatedAt)
                .ThenByDescending(task => task.CreatedAt)
                .ThenByDescending(task => task.Id)
        };
    }
}
