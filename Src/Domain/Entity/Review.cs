using Domain.AggregateRoot;
using Domain.Event;
using Domain.ValueObject;

namespace Domain.Entity;

public class Review : BaseAggregateRoot
{
    public User Author { get; private set; }
    public string Content { get; private set; }
    public double Rating { get; private set; }
    public int Likes { get; private set; }
    public int Dislikes { get; private set; }
    public string ReferenceId { get; init; }
    public ReferenceType ReferenceType { get; init; }

    private Review(
        User author,
        string content,
        double rating,
        string refId) : base()
    {
        Author = author;
        Content = content;
        Rating = rating;
        Likes = 0;
        Dislikes = 0;
        ReferenceId = refId;
    }

    private Review() { } // For AutoMapper

    public static Review Create(User author, string content, double rating, string refId)
    {
        var result = new Review(author, content, rating, refId);

        var @event = new ReviewCreatedEvent(author.Id, result.Id, content, rating, refId);
        result.AddDomainEvent(@event);
        return result;
    }

    public DomainEvent? UpdateRating(double newRating)
    {
        if (Rating == newRating)
            return null;

        Rating = newRating;
        var @event = new ReviewRatingUpdatedEvent(Author.Id, Id, newRating);
        AddDomainEvent(@event);

        return @event;
    }

    public DomainEvent? UpdateContent(string newContent)
    {
        if (Content == newContent)
            return null;

        var @event = new ReviewContentUpdatedEvent(Author.Id, Id, newContent);
        AddDomainEvent(@event);

        return @event;
    }

    public DomainEvent? AddLike(Guid userId)
    {
        var @event = new LikeAddedEvent(Id, userId);

        AddDomainEvent(@event);

        return @event;
    }

    public DomainEvent? AddDislike(Guid userId)
    {
        var @event = new DislikeAddedEvent(Id, userId);

        AddDomainEvent(@event);

        return @event;
    }
}
