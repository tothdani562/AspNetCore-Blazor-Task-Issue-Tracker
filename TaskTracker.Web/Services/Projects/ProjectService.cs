using Microsoft.EntityFrameworkCore;
using TaskTracker.Web.Data;
using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Dtos.Projects;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services.Projects;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectDetailsDto> CreateProjectAsync(Guid currentUserId, CreateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            OwnerId = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetProjectAsync(currentUserId, project.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Owner)
            .Include(project => project.Members)
                .ThenInclude(member => member.User)
            .Where(project => project.OwnerId == currentUserId || project.Members.Any(member => member.UserId == currentUserId))
            .OrderByDescending(project => project.UpdatedAt)
            .ToListAsync(cancellationToken);

        return projects.Select(MapSummary).ToList();
    }

    public async Task<ProjectDetailsDto> GetProjectAsync(Guid currentUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectAsync(projectId, includeMembers: true, tracking: false, cancellationToken);

        EnsureAccess(project, currentUserId);

        return MapDetails(project);
    }

    public async Task<ProjectDetailsDto> UpdateProjectAsync(Guid currentUserId, Guid projectId, UpdateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectAsync(projectId, includeMembers: true, tracking: true, cancellationToken);

        EnsureOwner(project, currentUserId);

        var hasChange = false;

        if (request.Name is not null)
        {
            var trimmedName = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                throw new ArgumentException("A projekt neve nem lehet ures.");
            }

            project.Name = trimmedName;
            hasChange = true;
        }

        if (request.Description is not null)
        {
            project.Description = NormalizeDescription(request.Description);
            hasChange = true;
        }

        if (!hasChange)
        {
            throw new ArgumentException("Legalabb egy mezot meg kell adni a modositashez.");
        }

        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetProjectAsync(currentUserId, projectId, cancellationToken);
    }

    public async Task DeleteProjectAsync(Guid currentUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectAsync(projectId, includeMembers: false, tracking: true, cancellationToken);

        EnsureOwner(project, currentUserId);

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDetailsDto> AddMemberAsync(Guid currentUserId, Guid projectId, AddProjectMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.MemberUserId == Guid.Empty)
        {
            throw new ArgumentException("A tag felhasznalo azonositoja kotelezo.");
        }

        var project = await LoadProjectAsync(projectId, includeMembers: true, tracking: true, cancellationToken);
        EnsureOwner(project, currentUserId);

        if (request.MemberUserId == project.OwnerId)
        {
            throw new ConflictException("A tulajdonos nem adhato hozza tagkent.");
        }

        if (project.Members.Any(member => member.UserId == request.MemberUserId))
        {
            throw new ConflictException("A felhasznalo mar tagja a projektnek.");
        }

        var memberUser = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == request.MemberUserId, cancellationToken);
        if (memberUser is null)
        {
            throw new NotFoundException("A megadott felhasznalo nem talalhato.");
        }

        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = memberUser.Id,
            JoinedAt = DateTime.UtcNow
        });

        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetProjectAsync(currentUserId, projectId, cancellationToken);
    }

    public async Task<ProjectDetailsDto> RemoveMemberAsync(Guid currentUserId, Guid projectId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectAsync(projectId, includeMembers: true, tracking: true, cancellationToken);
        EnsureOwner(project, currentUserId);

        if (memberUserId == project.OwnerId)
        {
            throw new ConflictException("A tulajdonos nem tavolithato el tagkent.");
        }

        var membership = project.Members.FirstOrDefault(member => member.UserId == memberUserId);
        if (membership is null)
        {
            throw new NotFoundException("A megadott tag nem talalhato a projektben.");
        }

        _dbContext.ProjectMembers.Remove(membership);
        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetProjectAsync(currentUserId, projectId, cancellationToken);
    }

    private async Task<Project> LoadProjectAsync(Guid projectId, bool includeMembers, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Project> query = _dbContext.Projects;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        query = query.Include(project => project.Owner);

        if (includeMembers)
        {
            query = query.Include(project => project.Members)
                .ThenInclude(member => member.User);
        }

        var project = await query.FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("A megadott projekt nem talalhato.");
        }

        return project;
    }

    private static void EnsureAccess(Project project, Guid currentUserId)
    {
        if (project.OwnerId == currentUserId)
        {
            return;
        }

        if (project.Members.Any(member => member.UserId == currentUserId))
        {
            return;
        }

        throw new ForbiddenException("Nincs jogosultsagod a projekt megtekintesere.");
    }

    private static void EnsureOwner(Project project, Guid currentUserId)
    {
        if (project.OwnerId != currentUserId)
        {
            throw new ForbiddenException("Csak a projekt tulajdonosa vegezheti ezt a muveletet.");
        }
    }

    private static ProjectListItemDto MapSummary(Project project)
    {
        return new ProjectListItemDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Owner = MapOwner(project.Owner),
            MemberCount = project.Members.Count,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    private static ProjectDetailsDto MapDetails(Project project)
    {
        return new ProjectDetailsDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Owner = MapOwner(project.Owner),
            MemberCount = project.Members.Count,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Members = project.Members
                .OrderBy(member => member.JoinedAt)
                .Select(member => new ProjectMemberDto
                {
                    UserId = member.UserId,
                    Email = member.User.Email,
                    FullName = member.User.FullName,
                    JoinedAt = member.JoinedAt
                })
                .ToList()
        };
    }

    private static AuthUserDto MapOwner(User owner)
    {
        return new AuthUserDto
        {
            Id = owner.Id,
            Email = owner.Email,
            FullName = owner.FullName
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
}
