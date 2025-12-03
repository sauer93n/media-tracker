using System.Text.Json;
using Application.Interface;
using Application.Model;
using Confluent.Kafka;
using Domain.Event;
using Microsoft.Extensions.Options;

namespace Application.EventPublisher;

public class EventPublisher : IEventPublisher
{
    private readonly IOptions<ApplicationOptions> applicationOptions;
    private readonly ProducerConfig config;

    public EventPublisher(IOptions<ApplicationOptions> applicationOptions)
    {
        this.applicationOptions = applicationOptions;
        config = new ProducerConfig
        {
            BootstrapServers = applicationOptions.Value.KafkaBroker.BootstrapServers,
        };
    }

    public async Task PublishAsync(DomainEvent domainEvent)
    {
        using var producer = new ProducerBuilder<string, string>(config).Build();
        var message = new Message<string, string>
        {
            Key = domainEvent.Id.ToString(),
            Value = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        };

        message.Headers = new() {
            { "EventType", System.Text.Encoding.UTF8.GetBytes(domainEvent.GetType().FullName ?? "Unknown") }
        };

        await producer.ProduceAsync(applicationOptions.Value.KafkaBroker.Topic, message);
    }
}