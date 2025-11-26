namespace Application.DTO;

public class UpdateReviewRequest
{
    public Guid ReviewId { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Rating { get; set; }
}