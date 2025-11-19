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
}