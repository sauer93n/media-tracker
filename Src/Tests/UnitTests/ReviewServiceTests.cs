using Application.DTO;
using Application.Interface;
using Application.Service;
using AutoMapper;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace UnitTests;

public class ReviewServiceTests : IDisposable
{
    private readonly ReviewContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IReviewService _reviewService;

    public ReviewServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ReviewContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReviewContext(options);
        _context.Database.EnsureCreated();

        // Setup mocks
        _eventPublisherMock = new Mock<IEventPublisher>();
        _mapperMock = new Mock<IMapper>();

        // Create service instance
        _reviewService = new ReviewService(_context, _eventPublisherMock.Object, _mapperMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateReviewAsync Tests

    [Fact]
    public async Task CreateReviewAsync_WithValidData_ShouldCreateReview()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var referenceId = Guid.NewGuid().ToString();
        var request = new CreateReviewRequest
        {
            AuthorId = authorId,
            AuthorName = "Test User",
            Content = "Great movie!",
            Rating = 8.5,
            ReferenceId = referenceId,
            ReferenceType = ReferenceType.Movie
        };

        var domainReview = Review.Create(
            new User(authorId, "Test User"),
            request.Content,
            request.Rating,
            referenceId
        );

        var reviewDto = new ReviewDTO
        {
            Id = domainReview.Id,
            AuthorId = authorId,
            Content = request.Content,
            Rating = request.Rating,
            ReferenceId = referenceId,
            ReferenceType = ReferenceType.Movie,
            Likes = 0,
            Dislikes = 0
        };

        var infrastructureReview = new Infrastructure.Entity.Review
        {
            Id = domainReview.Id,
            AuthorId = authorId,
            Content = request.Content,
            Rating = request.Rating,
            ReferenceId = referenceId,
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = [],
            Dislikes = []
        };

        _mapperMock.Setup(m => m.Map<Review>(It.IsAny<CreateReviewRequest>()))
            .Returns(domainReview);
        _mapperMock.Setup(m => m.Map<ReviewDTO>(It.IsAny<Review>()))
            .Returns(reviewDto);
        _mapperMock.Setup(m => m.Map<Infrastructure.Entity.Review>(It.IsAny<Review>()))
            .Returns(infrastructureReview);

        // Act
        var result = await _reviewService.CreateReviewAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Content.Should().Be(request.Content);
        result.Value.Rating.Should().Be(request.Rating);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<Domain.Event.DomainEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReviewAsync_WithInvalidRating_ShouldFail()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            AuthorId = Guid.NewGuid(),
            AuthorName = "Test User",
            Content = "Invalid rating",
            Rating = 6.0, // Invalid: should be 0-5
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = ReferenceType.Movie
        };

        // Act
        var result = await _reviewService.CreateReviewAsync(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateReviewAsync_WithEmptyContent_ShouldFail()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            AuthorId = Guid.NewGuid(),
            AuthorName = "Test User",
            Content = "", // Empty content
            Rating = 4.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = ReferenceType.Movie
        };

        // Act
        var result = await _reviewService.CreateReviewAsync(request);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    #endregion

    #region UpdateReviewAsync Tests

    [Fact]
    public async Task UpdateReviewAsync_WithValidData_ShouldUpdateReview()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var existingReview = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = authorId,
            Content = "Original content",
            Rating = 3.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(existingReview);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateReviewRequest
        {
            ReviewId = reviewId,
            Content = "Updated content",
            Rating = 4.5
        };

        var updatedDto = new ReviewDTO
        {
            Id = reviewId,
            AuthorId = authorId,
            Content = updateRequest.Content,
            Rating = updateRequest.Rating,
            Likes = 0,
            Dislikes = 0
        };

        var domainReview = Review.Create(
            new User(authorId, "Test User"),
            existingReview.Content,
            existingReview.Rating,
            existingReview.ReferenceId
        );

        _mapperMock.Setup(m => m.Map<ReviewDTO>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns(updatedDto);
        _mapperMock.Setup(m => m.Map<Review>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns(domainReview);
        _mapperMock.Setup(m => m.Map<ReviewDTO>(It.IsAny<Review>()))
            .Returns(updatedDto);

        // Act
        var result = await _reviewService.UpdateReviewAsync(authorId, updateRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Content.Should().Be(updateRequest.Content);
        result.Value.Rating.Should().Be(updateRequest.Rating);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<Domain.Event.DomainEvent>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenNotAuthor_ShouldFail()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var existingReview = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = authorId,
            Content = "Original content",
            Rating = 3.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(existingReview);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateReviewRequest
        {
            ReviewId = reviewId,
            Content = "Hacked content",
            Rating = 1.0
        };

        // Act
        var result = await _reviewService.UpdateReviewAsync(differentUserId, updateRequest);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenReviewNotFound_ShouldFail()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var nonExistentReviewId = Guid.NewGuid();

        var updateRequest = new UpdateReviewRequest
        {
            ReviewId = nonExistentReviewId,
            Content = "Updated content",
            Rating = 4.0
        };

        // Act
        var result = await _reviewService.UpdateReviewAsync(authorId, updateRequest);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("not found"));
    }

    #endregion

    #region DeleteReviewAsync Tests

    [Fact]
    public async Task DeleteReviewAsync_WithValidData_ShouldMarkAsDeleted()
    {
        // Arrange
        var authorId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            AuthorId = authorId,
            Content = "Content to delete",
            Rating = 3.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            IsDeleted = false,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        var added = _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.DeleteReviewAsync(authorId, added.Entity.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedReview = await _context.Reviews.FindAsync(added.Entity.Id);
        deletedReview.Should().NotBeNull();
        deletedReview!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenNotAuthor_ShouldFail()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = authorId,
            Content = "Protected content",
            Rating = 3.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.DeleteReviewAsync(reviewId, differentUserId);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    #endregion

    #region LikeReviewAsync Tests

    [Fact]
    public async Task LikeReviewAsync_FirstTime_ShouldAddLike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = Guid.NewGuid(),
            Content = "Likeable content",
            Rating = 4.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<Review>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns(Review.Create(
                new User(review.AuthorId, "Author"),
                review.Content,
                review.Rating,
                review.ReferenceId));

        // Act
        var result = await _reviewService.LikeReviewAsync(reviewId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var likeCount = await _context.Likes.CountAsync(l => l.ReviewId == reviewId && l.UserId == userId);
        likeCount.Should().Be(1);
    }

    [Fact]
    public async Task LikeReviewAsync_WhenAlreadyLiked_ShouldRemoveLike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = Guid.NewGuid(),
            Content = "Already liked content",
            Rating = 4.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>
            {
                new() { ReviewId = reviewId, UserId = userId }
            },
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.LikeReviewAsync(reviewId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var likeCount = await _context.Likes.CountAsync(l => l.ReviewId == reviewId && l.UserId == userId);
        likeCount.Should().Be(0);
    }

    [Fact]
    public async Task LikeReviewAsync_WhenReviewNotFound_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentReviewId = Guid.NewGuid();

        // Act
        var result = await _reviewService.LikeReviewAsync(nonExistentReviewId, userId);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    #endregion

    #region DislikeReviewAsync Tests

    [Fact]
    public async Task DislikeReviewAsync_FirstTime_ShouldAddDislike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = Guid.NewGuid(),
            Content = "Content to dislike",
            Rating = 2.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<Review>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns(Review.Create(
                new User(review.AuthorId, "Author"),
                review.Content,
                review.Rating,
                review.ReferenceId));

        // Act
        var result = await _reviewService.DislikeReviewAsync(reviewId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dislikeCount = await _context.Dislikes.CountAsync(d => d.ReviewId == reviewId && d.UserId == userId);
        dislikeCount.Should().Be(1);
    }

    [Fact]
    public async Task DislikeReviewAsync_WhenAlreadyDisliked_ShouldRemoveDislike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Infrastructure.Entity.Review
        {
            Id = reviewId,
            AuthorId = Guid.NewGuid(),
            Content = "Already disliked content",
            Rating = 2.0,
            ReferenceId = Guid.NewGuid().ToString(),
            ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
            Likes = new List<Infrastructure.Entity.Like>(),
            Dislikes = new List<Infrastructure.Entity.Dislike>
            {
                new() { ReviewId = reviewId, UserId = userId }
            }
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.DislikeReviewAsync(reviewId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dislikeCount = await _context.Dislikes.CountAsync(d => d.ReviewId == reviewId && d.UserId == userId);
        dislikeCount.Should().Be(0);
    }

    #endregion

    #region GetReviewsAsync Tests

    [Fact]
    public async Task GetReviewsAsync_WithMultipleReviews_ShouldReturnPagedResults()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User");

        // Add multiple reviews
        for (int i = 0; i < 15; i++)
        {
            var review = new Infrastructure.Entity.Review
            {
                Id = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = $"Review content {i}",
                Rating = 3.0 + (i % 3),
                ReferenceId = Guid.NewGuid().ToString(),
                ReferenceType = Infrastructure.Entity.ReferenceType.Movie,
                IsDeleted = false,
                Likes = new List<Infrastructure.Entity.Like>(),
                Dislikes = new List<Infrastructure.Entity.Dislike>()
            };
            _context.Reviews.Add(review);
        }
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<ReviewDTO>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns((Infrastructure.Entity.Review r) => new ReviewDTO
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                Likes = 0,
                Dislikes = 0
            });

        _mapperMock.Setup(m => m.Map<Review>(It.IsAny<Infrastructure.Entity.Review>()))
            .Returns((Infrastructure.Entity.Review r) => Review.Create(
                new User(r.AuthorId, "Author"),
                r.Content,
                r.Rating,
                r.ReferenceId));

        _mapperMock.Setup(m => m.Map<ReviewDTO>(It.IsAny<Review>()))
            .Returns((Review r) => new ReviewDTO
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                Likes = 0,
                Dislikes = 0
            });

        // Act
        var result = await _reviewService.GetReviewsAsync(user, pageNumber: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Data.Should().HaveCount(10); // First page
        result.Value.TotalCount.Should().Be(15);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetReviewsAsync_WithNoReviews_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User");

        // Act
        var result = await _reviewService.GetReviewsAsync(user, pageNumber: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Data.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion
}