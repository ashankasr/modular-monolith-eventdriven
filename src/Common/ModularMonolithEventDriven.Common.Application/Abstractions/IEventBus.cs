namespace ModularMonolithEventDriven.Common.Application.Abstractions;

/// <summary>
/// Abstraction over the message broker (MassTransit / RabbitMQ).
/// Inject this in domain event handlers to publish integration events to other modules.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class;
}
