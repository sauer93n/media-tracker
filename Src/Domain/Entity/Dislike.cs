namespace Domain.Entity;

public class Dislike
{

    public Guid ReviewId { get; private set; }

    public Guid UserId { get; private set; }

    public Dislike(Guid reviewId, Guid userId)
    {
        ReviewId = reviewId;
        UserId = userId;
    }    
}