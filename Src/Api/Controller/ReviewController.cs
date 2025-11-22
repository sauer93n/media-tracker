using Application.DTO;
using Application.Interface;
using Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewController(IReviewService reviewService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found in context");

        // Populate the request with user information
        request.AuthorId = domainUser.Id;
        request.AuthorName = domainUser.Name;

        var result = await reviewService.CreateReviewAsync(request);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpPost("{reviewId}/like")]
    public async Task<IActionResult> LikeReview([FromRoute] Guid reviewId)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found");

        var result = await reviewService.LikeReviewAsync(reviewId, domainUser.Id);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok();
    }

    [HttpPost("{reviewId}/dislike")]
    public async Task<IActionResult> DislikeReview([FromRoute] Guid reviewId)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found");

        var result = await reviewService.DislikeReviewAsync(reviewId, domainUser.Id);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok();
    }

    [HttpGet("")]
    public async Task<IActionResult> GetReviews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await reviewService.GetReviewsAsync(pageNumber, pageSize);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("type/{referenceType}")]
    public async Task<IActionResult> GetReviewsByType([FromRoute] ReferenceType referenceType, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await reviewService.GetReviewsForTypeAsync(referenceType, pageNumber, pageSize);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReviews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found");

        var result = await reviewService.GetUserReviewsAsync(domainUser.Id, pageNumber, pageSize);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetReviewDetails([FromRoute] Guid reviewId)
    {
        var result = await reviewService.GetReviewByIdAsync(reviewId);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found");

        var result = await reviewService.UpdateReviewAsync(domainUser.Id, request);
        
        if (result.HasException<UnauthorizedAccessException>())
            return Unauthorized(result.Errors);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview([FromRoute] Guid reviewId)
    {
        // Get the domain user from the middleware
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null) return Unauthorized("User not found");

        var result = await reviewService.DeleteReviewAsync(domainUser.Id, reviewId);

        if (result.HasException<UnauthorizedAccessException>())
            return Unauthorized(result.Errors);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok();
    }
}