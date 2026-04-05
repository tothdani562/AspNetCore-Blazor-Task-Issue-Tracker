using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskTracker.Web.Data;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.IntegrationTests;

public sealed class CommentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TaskTrackerCommentTests_{Guid.NewGuid():N}";

    public Guid OwnerId { get; } = Guid.Parse("10101010-1010-1010-1010-101010101010");
    public Guid MemberId { get; } = Guid.Parse("20202020-2020-2020-2020-202020202020");
    public Guid OutsiderId { get; } = Guid.Parse("30303030-3030-3030-3030-303030303030");
    public Guid ProjectId { get; } = Guid.Parse("40404040-4040-4040-4040-404040404040");
    public Guid TaskId { get; } = Guid.Parse("50505050-5050-5050-5050-505050505050");
    public Guid MemberCommentId { get; } = Guid.Parse("60606060-6060-6060-6060-606060606060");
    public Guid OwnerCommentId { get; } = Guid.Parse("70707070-7070-7070-7070-707070707070");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                options.DefaultScheme = TestAuthHandler.TestScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.TestScheme, _ => { });
        });
    }

    public void Seed()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var now = DateTime.UtcNow;

        var owner = new User
        {
            Id = OwnerId,
            Email = "owner@example.com",
            FullName = "Owner User",
            PasswordHash = "hash",
            CreatedAt = now,
            UpdatedAt = now
        };

        var member = new User
        {
            Id = MemberId,
            Email = "member@example.com",
            FullName = "Member User",
            PasswordHash = "hash",
            CreatedAt = now,
            UpdatedAt = now
        };

        var outsider = new User
        {
            Id = OutsiderId,
            Email = "outsider@example.com",
            FullName = "Outsider User",
            PasswordHash = "hash",
            CreatedAt = now,
            UpdatedAt = now
        };

        var project = new Project
        {
            Id = ProjectId,
            Name = "Backend Platform",
            Description = "Comment test project",
            OwnerId = OwnerId,
            CreatedAt = now,
            UpdatedAt = now,
            Members = new List<ProjectMember>
            {
                new()
                {
                    ProjectId = ProjectId,
                    UserId = MemberId,
                    JoinedAt = now
                }
            }
        };

        var task = new TaskItem
        {
            Id = TaskId,
            Title = "Implement comments backend",
            Description = "Iteration 11",
            Status = TaskItemStatus.TODO,
            Priority = TaskItemPriority.HIGH,
            ProjectId = ProjectId,
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-2)
        };

        var memberComment = new Comment
        {
            Id = MemberCommentId,
            TaskId = TaskId,
            AuthorId = MemberId,
            Content = "First seeded member comment",
            CreatedAt = now.AddHours(-3),
            UpdatedAt = now.AddHours(-3)
        };

        var ownerComment = new Comment
        {
            Id = OwnerCommentId,
            TaskId = TaskId,
            AuthorId = OwnerId,
            Content = "Second seeded owner comment",
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2)
        };

        dbContext.Users.AddRange(owner, member, outsider);
        dbContext.Projects.Add(project);
        dbContext.Tasks.Add(task);
        dbContext.Comments.AddRange(memberComment, ownerComment);
        dbContext.SaveChanges();
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }
}
