using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Api.Middleware;
using Api.Model;
using Application.EventPublisher;
using Application.Extensions;
using Application.Interface;
using Application.Model;
using Application.Service;
using Infrastructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CookieOptions = Api.Model.CookieOptions;

var builder = WebApplication.CreateBuilder(args);
var disableHttpsMetadata =
#if DEBUG
true;
#else
args.Contains("--no-https-metadata");
#endif
Console.WriteLine($"Disable HTTPS Metadata: {disableHttpsMetadata}");
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddKeyPerFile("/run/secrets", optional: true);

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
        options.RequireHttpsMetadata = !disableHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
        
        // options.Events = new JwtBearerEvents
        // {
        //     OnAuthenticationFailed = async context => {

        //     },
        //     OnTokenValidated = async context =>
        //     {
        //         var httpClient = context.HttpContext.RequestServices
        //             .GetRequiredService<IHttpClientFactory>()
        //             .CreateClient();
                
        //         var token = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        //         var introspectionUrl = $"{keycloakOptions.AuthServerUrl}/realms/{keycloakOptions.Realm}/protocol/openid-connect/token/introspect";
                
        //         var introspectionData = new Dictionary<string, string>
        //         {
        //             ["token"] = token,
        //             ["token_type_hint"] = "requesting_party_token",
        //             ["client_id"] = keycloakOptions.UserClientId,
        //             ["client_secret"] = keycloakOptions.UserClientSecret
        //         };
                
        //         var response = await httpClient.PostAsync(
        //             introspectionUrl,
        //             new FormUrlEncodedContent(introspectionData)
        //         );
                
        //         if (!response.IsSuccessStatusCode)
        //         {
        //             context.Fail("Token introspection failed");
        //             return;
        //         }
                
        //         var content = await response.Content.ReadAsStringAsync();
        //         using var doc = JsonDocument.Parse(content);
        //         var isActive = doc.RootElement.GetProperty("active").GetBoolean();
                
        //         if (!isActive)
        //         {
        //             context.Fail("Token is no longer active");
        //         }
        //     }
        // };
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
if (!disableHttpsMetadata) app.UseHttpsRedirection();

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
