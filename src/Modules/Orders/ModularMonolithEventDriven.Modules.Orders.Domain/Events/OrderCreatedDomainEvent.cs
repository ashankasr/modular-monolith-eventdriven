using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Orders.Domain.Events;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerId) : DomainEvent;
