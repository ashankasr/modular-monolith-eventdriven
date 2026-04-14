using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Inventory.Domain.Events;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain;

public sealed class StockReservation : AuditableGuidEntity
{
    private StockReservation() { }

    private StockReservation(Guid id, Guid orderId, List<ReservationItem> items) : base(id)
    {
        OrderId = orderId;
        Items = items;
        Status = ReservationStatus.Active;
    }

    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public List<ReservationItem> Items { get; private set; } = [];

    public static Result<StockReservation> Create(Guid id, Guid orderId, Guid correlationId, List<ReservationItem> items)
    {
        if (orderId == Guid.Empty)
            return Result.Failure<StockReservation>(Error.Validation("Inventory.InvalidOrderId", "Order ID cannot be empty."));

        if (items.Count == 0)
            return Result.Failure<StockReservation>(Error.Validation("Inventory.EmptyItems", "Reservation must have at least one item."));

        var reservation = new StockReservation(id, orderId, items);
        reservation.RaiseDomainEvent(new StockReservedDomainEvent(correlationId, orderId, id));
        return reservation;
    }

    // Choreography: status-only release, no domain event needed
    public void Release() => Status = ReservationStatus.Released;

    // Orchestration: raises StockReleasedDomainEvent so the outbox publishes StockReleasedEvent back to the saga
    public void Release(Guid correlationId)
    {
        Status = ReservationStatus.Released;
        RaiseDomainEvent(new StockReleasedDomainEvent(correlationId, OrderId, Id));
    }
}

public enum ReservationStatus { Active, Released }

public sealed record ReservationItem(Guid ProductId, int Quantity);
