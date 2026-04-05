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

public sealed class TaskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TaskTrackerTaskTests_{Guid.NewGuid():N}";

    public Guid OwnerId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public Guid MemberId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public Guid OutsiderId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public Guid ProjectId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public Guid TaskOneId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public Guid TaskTwoId { get; } = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

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
            Description = "Task test project",
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

        var taskOne = new TaskItem
        {
            Id = TaskOneId,
            Title = "Implement auth middleware",
            Description = "Security baseline",
            Status = TaskItemStatus.IN_PROGRESS,
            Priority = TaskItemPriority.HIGH,
            AssignedUserId = MemberId,
            DueDate = now.AddDays(3),
            ProjectId = ProjectId,
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-2)
        };

        var taskTwo = new TaskItem
        {
            Id = TaskTwoId,
            Title = "Write API docs",
            Description = "OpenAPI polishing",
            Status = TaskItemStatus.TODO,
            Priority = TaskItemPriority.MEDIUM,
            AssignedUserId = OwnerId,
            DueDate = now.AddDays(7),
            ProjectId = ProjectId,
            CreatedAt = now.AddHours(-3),
            UpdatedAt = now.AddHours(-1)
        };

        dbContext.Users.AddRange(owner, member, outsider);
        dbContext.Projects.Add(project);
        dbContext.Tasks.AddRange(taskOne, taskTwo);
        dbContext.SaveChanges();
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }
}
