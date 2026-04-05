using MediatR;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

/// <summary>
/// Marker interface for domain event handlers.
/// Implement this to react to a domain event dispatched via the Outbox processor.
/// The handler is responsible for converting the domain event to an integration event
/// and publishing it via IEventBus.
/// </summary>
public interface IDomainEventHandler<TDomainEvent>
    : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
