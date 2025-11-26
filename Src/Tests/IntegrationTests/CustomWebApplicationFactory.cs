using Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace MediaTracker.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that sets up a test environment with a real PostgreSQL database using Testcontainers.
/// This factory is used by integration tests to create an isolated test environment.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("mediatracker_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _postgresContainer.GetConnectionString();

    /// <summary>
    /// Initializes the test container before any tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    /// <summary>
    /// Cleans up the test container after all tests complete.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Configures the web host to use the test database and other test-specific settings.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<ReviewContext>));
            services.RemoveAll(typeof(ReviewContext));

            // Add DbContext with test database connection string
            services.AddDbContext<ReviewContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            // Build the service provider to apply migrations
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReviewContext>();
            
            // Apply migrations to ensure database schema is up to date
            dbContext.Database.Migrate();
        });

        // Override environment to Testing
        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates a new authenticated HTTP client for testing authenticated endpoints.
    /// </summary>
    /// <param name="token">JWT token to use for authentication</param>
    /// <returns>HttpClient configured with Authorization header</returns>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    /// <summary>
    /// Gets a scoped service from the test server's service provider.
    /// Useful for accessing database context or other services in tests.
    /// </summary>
    public T GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
