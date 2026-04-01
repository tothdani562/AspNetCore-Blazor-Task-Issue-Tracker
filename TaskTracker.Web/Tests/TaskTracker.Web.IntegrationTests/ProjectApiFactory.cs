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

public sealed class ProjectApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TaskTrackerTests_{Guid.NewGuid():N}";

    public Guid OwnerId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid MemberId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public Guid OutsiderId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public Guid ProjectId { get; } = Guid.Parse("44444444-4444-4444-4444-444444444444");

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
            Description = "Iteration test project",
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

        dbContext.Users.AddRange(owner, member, outsider);
        dbContext.Projects.Add(project);
        dbContext.SaveChanges();
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }
}
