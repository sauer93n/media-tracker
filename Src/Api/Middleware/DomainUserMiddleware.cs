using System.Security.Claims;
using Domain.Entity;

namespace Api.Middleware;

/// <summary>
/// Middleware to extract the domain User from the authenticated HttpContext and store it for later use
/// </summary>
public class DomainUserMiddleware
{
    private readonly RequestDelegate next;

    public DomainUserMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process authenticated requests
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var userId = context.User.FindFirst("sub")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("user_id");
            var userName = context.User.FindFirst("name")?.Value ?? context.User.FindFirst("preferred_username")?.Value ?? "Unknown";

            if (!string.IsNullOrEmpty(userId.Value) && Guid.TryParse(userId.Value, out var parsedUserId))
            {
                // Create the domain User model
                var domainUser = new User(parsedUserId, userName);

                // Store in HttpContext.Items for access in controllers/services
                context.Items["DomainUser"] = domainUser;
            }
        }

        await next(context);
    }
}

/// <summary>
/// Extension method to add the DomainUserMiddleware to the pipeline
/// </summary>
public static class DomainUserMiddlewareExtensions
{
    public static IApplicationBuilder UseDomainUserMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DomainUserMiddleware>();
    }
}
