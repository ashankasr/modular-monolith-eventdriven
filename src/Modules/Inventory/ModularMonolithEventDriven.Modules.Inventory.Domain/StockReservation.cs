using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain;

public sealed class StockReservation : AuditableGuidEntity
{
    private StockReservation() { }

    public StockReservation(Guid id, Guid orderId, List<ReservationItem> items) : base(id)
    {
        OrderId = orderId;
        Items = items;
        Status = ReservationStatus.Active;
    }

    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public List<ReservationItem> Items { get; private set; } = [];

    public void Release() => Status = ReservationStatus.Released;
}

public enum ReservationStatus { Active, Released }

public sealed record ReservationItem(Guid ProductId, int Quantity);
