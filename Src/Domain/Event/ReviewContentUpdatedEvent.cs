namespace Domain.Event;

public record ReviewContentUpdatedEvent(Guid userId, Guid reviewId, string newContent) : DomainEvent;