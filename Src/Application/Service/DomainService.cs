using Application.Interface;
using Domain.AggregateRoot;

namespace Application.Service;

public abstract class DomainService(IEventPublisher eventPublisher)
{
    protected async Task PublishEventAsync(BaseAggregateRoot aggregateRoot)
    {
        foreach (var domainEvent in aggregateRoot.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent);
        }

        aggregateRoot.ClearDomainEvents();
    }
}