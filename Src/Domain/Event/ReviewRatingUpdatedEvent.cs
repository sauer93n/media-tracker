namespace Domain.Event;

public record ReviewRatingUpdatedEvent(Guid UserId, Guid ReviewId, double NewRating) : DomainEvent;