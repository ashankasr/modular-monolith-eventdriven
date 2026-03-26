namespace ModularMonolithEventDriven.Modules.Orders.Domain;

public enum OrderStatus
{
    Pending = 0,
    StockReserved = 1,
    PaymentProcessed = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
