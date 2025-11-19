namespace Domain.Entity;

public class Like
{
    public Guid ReviewId { get; private set; }

    public Guid UserId { get; private set; }

    public Like(Guid reviewId, Guid userId)
    {
        ReviewId = reviewId;
        UserId = userId;
    }    
}