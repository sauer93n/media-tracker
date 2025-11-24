namespace Application.DTO;

public class CreateReviewRequest
{
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Rating { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}