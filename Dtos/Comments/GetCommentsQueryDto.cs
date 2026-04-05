using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Dtos.Comments;

public class GetCommentsQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
