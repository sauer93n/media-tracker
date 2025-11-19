using Domain.Entity;
using Domain.Event;

namespace Domain.AggregateRoot;

public abstract class BaseAggregateRoot : BaseEntity
{
    private readonly List<DomainEvent> domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => domainEvents;

    public void AddDomainEvent(DomainEvent domainEvent) => domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => domainEvents.Clear();
}