using Application.DTO;
using Application.Interface;
using Gridify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpGet("{referenceType}/{referenceId}")]
    public async Task<IActionResult> GetMediaDetails([FromRoute] ReferenceType referenceType, [FromRoute] string referenceId, CancellationToken cancellationToken)
    {
        var result = await mediaService.GetMediaDetailsAsync(referenceId, referenceType, cancellationToken);

        if (result.IsFailed) return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("search/{referenceType}")]
    public async Task<IActionResult> SearchMedia([FromRoute] ReferenceType referenceType, [FromQuery] string query, [FromQuery] GridifyQuery gridifyQuery, CancellationToken cancellationToken)
    {
        var result = await mediaService.SearchMediaAsync(query, referenceType, cancellationToken);

        if (result.IsFailed) return BadRequest(result.Errors);

        // Apply Gridify pagination and filtering
        var queryable = result.Value.AsQueryable();
        var filteredQuery = queryable.ApplyFiltering(gridifyQuery);
        var pageNumber = gridifyQuery.Page > 0 ? gridifyQuery.Page : 1;
        var pageSize = gridifyQuery.PageSize > 0 ? gridifyQuery.PageSize : 10;

        var totalCount = filteredQuery.Count();
        var paginatedResult = filteredQuery
            .ApplyPaging(gridifyQuery)
            .ToList();

        var pagedResult = new PagedResult<MediaDetailsDTO>
        {
            Data = paginatedResult,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(pagedResult);
    }

    [HttpGet("poster/{referenceType}/{referenceId}")]
    public async Task<IActionResult> GetPosterImage([FromRoute] ReferenceType referenceType,
        [FromRoute] string referenceId,
        CancellationToken cancellationToken)
    {
        var result = await mediaService.GetPosterImageAsync(referenceId, referenceType, cancellationToken);

        if (result.IsFailed) return BadRequest(result.Errors);

        return File(result.Value, "image/jpeg");
    }

    
}