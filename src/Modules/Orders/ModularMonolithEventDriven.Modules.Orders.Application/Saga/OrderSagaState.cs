using MassTransit;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Saga;

public sealed class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
