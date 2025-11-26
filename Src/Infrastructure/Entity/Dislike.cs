namespace Infrastructure.Entity;

public class Dislike
{
    public Guid ReviewId { get; set; }

    public Guid UserId { get; set; }
}