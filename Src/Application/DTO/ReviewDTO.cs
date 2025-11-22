namespace Application.DTO;

public class ReviewDTO
{
    public Guid AuthorId { get; set; }
    public string Content { get; set; }
    public double Rating { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public string ReferenceId { get; set; } 
    public ReferenceType ReferenceType { get; set; }   
}