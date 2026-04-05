using MediatR;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

/// <summary>
/// Wraps a domain event so MediatR can dispatch it without coupling Common.Domain to MediatR.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
