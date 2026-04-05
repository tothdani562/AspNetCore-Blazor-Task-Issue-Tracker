namespace TaskTracker.Web.Dtos.Projects;

public class ProjectDetailsDto : ProjectListItemDto
{
    public List<ProjectMemberDto> Members { get; set; } = new();
}
