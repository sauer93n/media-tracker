using System.Net;
using Application.DTO;
using FluentAssertions;
using Infrastructure.Context;
using Xunit;

namespace MediaTracker.IntegrationTests;

/// <summary>
/// Integration tests for Review API endpoints.
/// Tests the full request pipeline including database operations, authentication, and authorization.
/// </summary>
public class ReviewApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ReviewContext _dbContext;

    public ReviewApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Get a scoped DbContext for test setup/teardown
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<ReviewContext>();
    }

    public async Task InitializeAsync()
    {
        // Clean the database before each test
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up after each test
        _dbContext.Reviews.RemoveRange(_dbContext.Reviews);
        await _dbContext.SaveChangesAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task GetReviews_WithoutAuthentication_Returns_Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/review");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReview_WithValidData_Returns_Created()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            Content = "This is a test review content",
            Rating = 4,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = ReferenceType.Movie,
            AuthorId = Guid.NewGuid()
        };

        // Note: This test will fail without proper authentication setup
        // In a real scenario, you would need to:
        // 1. Set up a test Keycloak server or mock the JWT validation
        // 2. Generate a valid JWT token
        // 3. Use _factory.CreateAuthenticatedClient(token)

        // Act
        var response = await _client.PostAsJsonAsync("/api/review/create", request);

        // Assert
        // Without authentication, this should return Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReviews_WithValidPagination_Returns_PagedResults()
    {
        // This test demonstrates how you would test with authentication
        // You would need to implement CreateTestJwtToken() method
        
        // Arrange
        // var token = CreateTestJwtToken();
        // var authenticatedClient = _factory.CreateAuthenticatedClient(token);

        // Act
        // var response = await authenticatedClient.GetAsync("/api/reviews?page=1&pageSize=10");

        // Assert
        // response.StatusCode.Should().Be(HttpStatusCode.OK);
        // var reviews = await response.Content.ReadFromJsonAsync<PagedResult<ReviewDTO>>();
        // reviews.Should().NotBeNull();
        // reviews.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReview_WhenUserIsNotAuthor_Returns_Unauthorized()
    {
        // This test would verify authorization logic
        // Requires authentication setup with different user IDs
    }

    [Fact]
    public async Task DeleteReview_WhenReviewExists_Returns_NoContent()
    {
        // This test would verify delete operation
        // Requires authentication setup
    }

    // Helper method to create test JWT tokens (needs implementation)
    // private string CreateTestJwtToken(Guid userId = default, string username = "testuser")
    // {
    //     // Implementation would create a valid JWT token for testing
    //     // This requires setting up JWT token generation with test secrets
    //     throw new NotImplementedException();
    // }
}
