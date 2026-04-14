using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;

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

    public static Result<StockReservation> Create(Guid id, Guid orderId, List<ReservationItem> items)
    {
        if (orderId == Guid.Empty)
            return Result.Failure<StockReservation>(Error.Validation("Inventory.InvalidOrderId", "Order ID cannot be empty."));

        if (items.Count == 0)
            return Result.Failure<StockReservation>(Error.Validation("Inventory.EmptyItems", "Reservation must have at least one item."));

        return new StockReservation(id, orderId, items);
    }

    public void Release() => Status = ReservationStatus.Released;
}

public enum ReservationStatus { Active, Released }

public sealed record ReservationItem(Guid ProductId, int Quantity);
