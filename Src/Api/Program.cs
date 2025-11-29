using Api.Middleware;
using Api.Model;
using Application.EventPublisher;
using Application.Extensions;
using Application.Interface;
using Application.Model;
using Application.Service;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using CookieOptions = Api.Model.CookieOptions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });
    }
);
builder.Services.AddControllers();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// Add CORS configuration from appsettings
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));
var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders(corsOptions.ExposedHeaders);
    });
});
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
var connectionString = builder.Configuration.GetConnectionString("MediaTracker");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ReviewContext>(options =>
        options.UseNpgsql(connectionString));

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString,
            name: "postgresql",
            timeout: TimeSpan.FromSeconds(3),
            tags: ["db", "sql", "postgresql"]
        );
}

builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Add HttpClient factory for use in controllers
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("middleware").AddResiliencePolicies();

var keycloakOptionsSection = builder.Configuration.GetSection("KeycloakOptions");
if (keycloakOptionsSection.Exists())
{
    var keycloakOptions = keycloakOptionsSection.Get<KeycloakOptions>();
    builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = $"{keycloakOptions.AuthServerUrl}/realms/{keycloakOptions.Realm}";
        options.Audience = "account";
#if DEBUG
        options.RequireHttpsMetadata = false;
#endif
    });
    builder.Services.Configure<KeycloakOptions>(keycloakOptionsSection);
}

var applicationOptionsSection = builder.Configuration.GetSection("ApplicationOptions");
if (applicationOptionsSection.Exists())
    builder.Services.Configure<ApplicationOptions>(applicationOptionsSection);

var cookieOptionsSection = builder.Configuration.GetSection("CookieOptions");
if (cookieOptionsSection.Exists())
    builder.Services.Configure<CookieOptions>(cookieOptionsSection);

builder.Services.AddAuthorization();
builder.Services.AddAutoMapperProfiles(typeof(IReviewService).Assembly);

var app = builder.Build();
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseCookieTokenMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.UseDomainUserMiddleware();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Only checks application is running
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") // Only checks dependencies like DB
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Media Tracker API v1");
    });
}
else
{
    using var scope = app.Services.CreateScope();

    // Validate required services exist
    var dbContext = scope.ServiceProvider.GetService<ReviewContext>();
    if (dbContext == null)
        throw new InvalidOperationException("Database context not configured");
}

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
