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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
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
var connectionString =
    builder.Configuration.GetConnectionString("MediaTracker")
        ?? throw new InvalidOperationException("Connection string 'MediaTracker' not found.");

builder.Services.AddDbContext<ReviewContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Add HttpClient factory for use in controllers
builder.Services.AddHttpClient();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("KeycloakOptions"));
builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("ApplicationOptions"));
builder.Services.Configure<CookieOptions>(builder.Configuration.GetSection("CookieOptions"));

var keycloakOptions = builder.Configuration.GetSection("KeycloakOptions").Get<KeycloakOptions>()
    ?? throw new InvalidOperationException("KeycloakOptions not configured");

builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = $"{keycloakOptions.AuthServerUrl}/realms/{keycloakOptions.Realm}";
        options.Audience = "account";
        #if DEBUG
        options.RequireHttpsMetadata = false;
        #endif
    });
builder.Services.AddAuthorization();
builder.Services.AddAutoMapperProfiles(typeof(IReviewService).Assembly);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowFrontend");
app.UseDomainUserMiddleware();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Media Tracker API v1");
    });
}

app.UseHttpsRedirection();

app.Run();
