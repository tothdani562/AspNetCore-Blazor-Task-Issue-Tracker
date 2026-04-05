using TaskTracker.Web.Components;
using TaskTracker.Web.Configuration;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Data;
using TaskTracker.Web.Auth;
using TaskTracker.Web.Dtos;
using TaskTracker.Web.Middleware;
using TaskTracker.Web.Services;
using TaskTracker.Web.Services.Auth;
using TaskTracker.Web.Services.Comments;
using TaskTracker.Web.Services.Projects;
using TaskTracker.Web.Services.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AppCorsOptions>()
    .Bind(builder.Configuration.GetSection(AppCorsOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<AuthRateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(AuthRateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? new DatabaseOptions();

var corsOptions = builder.Configuration
    .GetSection(AppCorsOptions.SectionName)
    .Get<AppCorsOptions>() ?? new AppCorsOptions();

var authRateLimitingOptions = builder.Configuration
    .GetSection(AuthRateLimitingOptions.SectionName)
    .Get<AuthRateLimitingOptions>() ?? new AuthRateLimitingOptions();

// Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            if (databaseOptions.EnableRetryOnFailure)
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: databaseOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);
            }

            npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
        }));

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") && corsOptions.AllowedOrigins.Count == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development and Testing environments.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("TaskTrackerCors", policy =>
    {
        if (corsOptions.AllowedOrigins.Count == 0)
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader();

        if (corsOptions.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

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

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") && IsDevelopmentSigningKey(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("A development JWT signing key nem hasznalhato ebben a kornyezetben. Allits be biztonsagos kulcsot a Jwt__SigningKey environment valtozoban.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Success = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Hianyzik vagy ervenytelen a bearer token.",
                    Path = context.HttpContext.Request.Path,
                    Timestamp = DateTime.UtcNow
                });
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Success = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Nincs jogosultsagod a kert eroforras eleresehez.",
                    Path = context.Request.Path,
                    Timestamp = DateTime.UtcNow
                });
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-endpoints", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetRateLimitPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitingOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitingOptions.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Success = false,
            StatusCode = StatusCodes.Status429TooManyRequests,
            Message = "Tul sok auth kerest kuldtel rovid idon belul. Probald ujra kesobb.",
            Path = context.HttpContext.Request.Path,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    };
});

builder.Services.AddAuthorization();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddHttpClient("TaskTrackerApi");
builder.Services.AddScoped<IAuthSessionStore, AuthSessionStore>();
builder.Services.AddScoped<IAuthTokenStorage, BrowserAuthTokenStorage>();
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<IProjectsApiClient, ProjectsApiClient>();
builder.Services.AddScoped<ITasksApiClient, TasksApiClient>();
builder.Services.AddScoped<ICommentsApiClient, CommentsApiClient>();
builder.Services.AddScoped<AuthFacade>();
builder.Services.AddScoped<AppAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    serviceProvider => serviceProvider.GetRequiredService<AppAuthenticationStateProvider>());

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

if (!app.Environment.IsEnvironment("Testing"))
{
    // Database initialization
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            if (dbContext.Database.IsRelational())
            {
                app.Logger.LogInformation("Applying database migrations...");
                dbContext.Database.Migrate();
                app.Logger.LogInformation("Database migrations applied successfully.");

                EnsureProjectSchema(dbContext, app.Logger);
            }
            else
            {
                app.Logger.LogInformation("Skipping migrations because the current provider is not relational.");
            }

            // Seed initial data if needed
            SeedData(dbContext, app.Logger);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while initializing the database.");

            if (databaseOptions.FailOnStartupError)
            {
                throw;
            }

            app.Logger.LogCritical(
                "Application startup continues despite database initialization failure because Database:FailOnStartupError=false. API calls that require DB access may fail until the database becomes available.");
        }
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
app.UseAuthAuditLogging();

app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseCors("TaskTrackerCors");
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

static void EnsureProjectSchema(ApplicationDbContext dbContext, ILogger logger)
{
    logger.LogWarning("Projects tables are missing. Recreating project schema.");

    dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""Projects"" (
    ""Id"" uuid NOT NULL,
    ""Name"" character varying(100) NOT NULL,
    ""Description"" character varying(1000) NULL,
    ""OwnerId"" uuid NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""UpdatedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_Projects"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_Projects_Users_OwnerId"" FOREIGN KEY (""OwnerId"") REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ""IX_Projects_OwnerId"" ON ""Projects"" (""OwnerId"");

CREATE TABLE IF NOT EXISTS ""ProjectMembers"" (
    ""ProjectId"" uuid NOT NULL,
    ""UserId"" uuid NOT NULL,
    ""JoinedAt"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_ProjectMembers"" PRIMARY KEY (""ProjectId"", ""UserId""),
    CONSTRAINT ""FK_ProjectMembers_Projects_ProjectId"" FOREIGN KEY (""ProjectId"") REFERENCES ""Projects"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_ProjectMembers_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_ProjectMembers_UserId"" ON ""ProjectMembers"" (""UserId"");
    ");

    logger.LogInformation("Project schema recreated successfully.");
}

static string GetRateLimitPartitionKey(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static bool IsDevelopmentSigningKey(string signingKey)
{
    return string.IsNullOrWhiteSpace(signingKey)
        || signingKey.Contains("DEVELOPMENT_ONLY", StringComparison.OrdinalIgnoreCase)
        || signingKey.StartsWith("THIS_IS_A_DEVELOPMENT", StringComparison.OrdinalIgnoreCase);
}

public partial class Program
{
}

