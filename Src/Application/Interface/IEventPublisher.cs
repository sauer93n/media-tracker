using Domain.Event;

namespace Application.Interface;

public interface IEventPublisher
{
    Task PublishAsync(DomainEvent @event);
}