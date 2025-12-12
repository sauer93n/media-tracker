using Api.Extensions;
using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KinopoiskController(
    ILogger<KinopoiskController> logger,
    IKinopoiskService kinopoiskService) : ControllerBase
{
    /// <summary>
    /// Imports user ratings from Kinopoisk, converts them to reviews, and saves to database
    /// </summary>
    /// <param name="userId">The Kinopoisk user ID</param>
    /// <returns>Collection of created reviews with TMDb enrichment</returns>
    [HttpPost("import-ratings/{userId}")]
    public async Task<IActionResult> ImportAndConvertRatings([FromRoute] string userId)
    {
        var domainUser = HttpContext.GetDomainUser();
        if (domainUser == null)
            return Unauthorized("User not found");

        logger.LogInformation("Importing and converting ratings for Kinopoisk user {UserId} to reviews for app user {AppUserId}", 
            userId, domainUser.Id);

        // Step 1: Import ratings from Kinopoisk
        var ratingsResult = await kinopoiskService.ImportUserRatings(userId);

        if (ratingsResult.IsFailed)
            return BadRequest(new { error = "Failed to import ratings", details = ratingsResult.Errors });

        logger.LogInformation("Successfully imported {Count} ratings from Kinopoisk", ratingsResult.Value.Count());

        // Step 2: Convert ratings to reviews and save to database
        var reviewsResult = await kinopoiskService.ConvertRatingsToReviews(
            ratingsResult.Value, 
            domainUser);

        if (reviewsResult.IsFailed)
            return BadRequest(new { error = "Failed to convert ratings to reviews", details = reviewsResult.Errors });

        logger.LogInformation("Successfully converted {Count} ratings to reviews", reviewsResult.Value.Count());

        return Ok(new
        {
            totalImported = ratingsResult.Value.Count(),
            totalConverted = reviewsResult.Value.Count(),
            reviews = reviewsResult.Value
        });
    }

    /// <summary>
    /// Finds media information by Kinopoisk ID
    /// </summary>
    /// <param name="kinopoiskId">The Kinopoisk ID</param>
    /// <returns>Media details from TMDb</returns>
    [HttpGet("media/{kinopoiskId}")]
    public async Task<IActionResult> FindMediaByKinopoiskId([FromRoute] int kinopoiskId)
    {
        logger.LogInformation("Looking up media for Kinopoisk ID: {KinopoiskId}", kinopoiskId);

        var result = await kinopoiskService.FindMediaByKinopoiskId(kinopoiskId);

        if (result.IsFailed)
            return NotFound(new { error = "Media not found", details = result.Errors });

        return Ok(result.Value);
    }
}