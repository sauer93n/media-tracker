namespace Domain.Event;

public record LikeAddedEvent(Guid ReviewId, Guid UserId) : DomainEvent;