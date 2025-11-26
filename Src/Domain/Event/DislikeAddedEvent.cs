namespace Domain.Event;

public record DislikeAddedEvent(Guid ReviewId, Guid UserId) : DomainEvent;