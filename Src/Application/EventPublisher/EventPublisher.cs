using Application.Interface;
using Domain.Event;

namespace Application.EventPublisher;

public class EventPublisher : IEventPublisher
{
    public async Task PublishAsync(DomainEvent domainEvent)
    {
        // Implementation for publishing the event
    }
}