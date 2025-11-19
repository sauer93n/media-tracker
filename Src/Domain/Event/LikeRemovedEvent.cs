namespace Domain.Event;

public record LikeRemovedEvent(Guid ReviewId, Guid UserId) : DomainEvent;