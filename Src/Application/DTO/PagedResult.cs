namespace Application.DTO;

/// <summary>
/// Represents a paginated result set
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
