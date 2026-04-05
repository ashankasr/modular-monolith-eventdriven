using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Common.Infrastructure.EventBus;

/// <summary>
/// IEventBus implementation backed by MassTransit.
/// Domain event handlers inject IEventBus (not IPublishEndpoint directly)
/// so the Application layer stays free of MassTransit references.
/// </summary>
public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
