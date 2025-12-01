using Domain.ValueObject;

namespace Domain.Event;

public record ReviewCreatedEvent(Guid UserId, Guid ReviewId, string Content, double Rating, string ReferenceId, ReferenceType ReferenceType) : DomainEvent;
