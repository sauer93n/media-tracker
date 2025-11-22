using Application.DTO;
using Application.Interface;
using Api.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentResults;

namespace MediaTracker.Tests
{
    public class ReviewControllerTests
    {
        private readonly Mock<IReviewService> _reviewServiceMock;
        private readonly ReviewController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly string _testUserName = "Test User";

        public ReviewControllerTests()
        {
            _reviewServiceMock = new Mock<IReviewService>();
            
            // Mock HttpContext and User
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", _testUserId.ToString()),
                new Claim("preferred_username", _testUserName)
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };

            _controller = new ReviewController(_reviewServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        [Fact]
        public async Task CreateReview_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new CreateReviewRequest
            {
                Content = "This is a great movie!",
                Rating = 5,
                ReferenceId = "12345"
            };

            var expectedDto = new ReviewDTO
            {
                AuthorId = _testUserId,
                Content = request.Content,
                Rating = request.Rating,
                ReferenceId = request.ReferenceId
            };

            _reviewServiceMock
                .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
                .ReturnsAsync(Result.Ok(expectedDto));

            // Act
            var result = await _controller.CreateReview(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeOfType<ReviewDTO>().Subject;
            returnedDto.Should().BeEquivalentTo(expectedDto);
            
            // Verify that the AuthorId and AuthorName were set correctly from HttpContext
            _reviewServiceMock.Verify(s => s.CreateReviewAsync(It.Is<CreateReviewRequest>(
                r => r.AuthorId == _testUserId && r.AuthorName == _testUserName)), Times.Once);
        }

        [Fact]
        public async Task UpdateReview_WhenUserIsAuthorized_ShouldReturnOk()
        {
            // Arrange
            var request = new UpdateReviewRequest
            {
                ReviewId = Guid.NewGuid(),
                Content = "Updated content",
                Rating = 4
            };

            var updatedDto = new ReviewDTO { Content = request.Content, Rating = request.Rating };

            _reviewServiceMock
                .Setup(s => s.UpdateReviewAsync(_testUserId, request))
                .ReturnsAsync(Result.Ok(updatedDto));

            // Act
            var result = await _controller.UpdateReview(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(updatedDto);
        }

        [Fact]
        public async Task UpdateReview_WhenUserIsNotAuthorized_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new UpdateReviewRequest
            {
                ReviewId = Guid.NewGuid(),
                Content = "Updated content",
                Rating = 4
            };

            _reviewServiceMock
                .Setup(s => s.UpdateReviewAsync(_testUserId, request))
                .ReturnsAsync(Result.Fail(new ExceptionalError(new UnauthorizedAccessException())));

            // Act
            var result = await _controller.UpdateReview(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
