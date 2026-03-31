using TaskTracker.Web.Components;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Data;
using TaskTracker.Web.Auth;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// API Controllers
builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"Invalid value for '{x.Key}'."
                    : error.ErrorMessage))
            .ToArray();

        return new BadRequestObjectResult(new ErrorResponse
        {
            Success = false,
            StatusCode = StatusCodes.Status400BadRequest,
            Message = errors.Length > 0 ? string.Join(" ", errors) : "Validation failed.",
            Path = context.HttpContext.Request.Path,
            Timestamp = DateTime.UtcNow
        });
    };
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v0.1.0",
        Description = "Task / Issue Tracker API alapú implementáció Blazor + ASP.NET Core stackkel"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Apply pending migrations
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            app.Logger.LogInformation("Applying pending migrations...");
            dbContext.Database.Migrate();
            app.Logger.LogInformation("Migrations applied successfully.");
        }
        else
        {
            app.Logger.LogInformation("No pending migrations found.");
        }

        // Seed initial data if needed
        SeedData(dbContext, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTracker API V0.1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Globális exception handling middleware
app.UseExceptionHandlingMiddleware();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// API routes
app.MapControllers();

// Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Seed initial data
static void SeedData(ApplicationDbContext dbContext, ILogger logger)
{
    try
    {
        // Check if users already exist
        if (!dbContext.Users.Any())
        {
            logger.LogInformation("Seeding initial data...");
            
            // Sample users with pre-hashed passwords for testing
            var users = new[]
            {
                new TaskTracker.Web.Models.User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@example.com",
                    FullName = "Admin User",
                    PasswordHash = "$2a$11$ZGH4lKOG3lUcArnVR7/Xeu1k6ApoQu7sMKTYSEBQGnMg1m8vfhHLi", // admin123
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TaskTracker.Web.Models.User
                {
                    Id = Guid.NewGuid(),
                    Email = "user@example.com",
                    FullName = "Test User",
                    PasswordHash = "$2a$11$jZkGVa6UtbsyOXE8FlGqd.MpkMBH0cFH/P8J2xqZKTn2wlMKSw1FO", // user123 
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
            
            logger.LogInformation($"Seeded {users.Length} users.");
        }
        else
        {
            logger.LogInformation("Database already contains data. Skipping seed.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw;
    }
}

