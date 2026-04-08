using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Saga;

public sealed class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    private readonly ILogger<OrderSaga> _logger;

    public State OrderSubmitted { get; private set; } = null!;
    public State StockReserved { get; private set; } = null!;
    public State PaymentProcessed { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderSagaStartMessage> OrderSagaStarted { get; private set; } = null!;
    public Event<StockReservedEvent> StockWasReserved { get; private set; } = null!;
    public Event<StockReservationFailedEvent> StockReservationFailed { get; private set; } = null!;
    public Event<PaymentProcessedEvent> PaymentWasProcessed { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;

    public OrderSaga(ILogger<OrderSaga> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => OrderSagaStarted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockWasReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReservationFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentWasProcessed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(OrderSagaStarted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.SimulatePaymentFailure = ctx.Message.SimulatePaymentFailure;
                    ctx.Saga.SimulateStockFailure = ctx.Message.SimulateStockFailure;
                    ctx.Saga.StartedAt = DateTime.UtcNow;
                    _logger.LogInformation("[SAGA] Order {OrderId} saga started. Sending ReserveStockCommand.", ctx.Saga.OrderId);
                })
                .TransitionTo(OrderSubmitted)
                .PublishAsync(ctx => ctx.Init<ReserveStockCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Message.Items
                })));

        During(OrderSubmitted,
            When(StockWasReserved)
                .Then(ctx =>
                {
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    _logger.LogInformation("[SAGA] Stock reserved for Order {OrderId}. ReservationId: {ReservationId}. Sending ProcessPaymentCommand.", ctx.Saga.OrderId, ctx.Message.ReservationId);
                })
                .TransitionTo(StockReserved)
                .PublishAsync(ctx => ctx.Init<ProcessPaymentCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.CustomerId,
                    ctx.Saga.CustomerEmail,
                    Amount = ctx.Saga.TotalAmount
                })),

            When(StockReservationFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    _logger.LogWarning("[SAGA] Stock reservation FAILED for Order {OrderId}. Reason: {Reason}", ctx.Saga.OrderId, ctx.Message.Reason);
                })
                .TransitionTo(Failed)
                .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.CustomerEmail,
                    Status = "Failed",
                    Message = $"Your order failed: {ctx.Saga.FailureReason}"
                }))
                .Finalize());

        During(StockReserved,
            When(PaymentWasProcessed)
                .Then(ctx =>
                {
                    ctx.Saga.PaymentId = ctx.Message.PaymentId;
                    ctx.Saga.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("[SAGA] Payment processed for Order {OrderId}. PaymentId: {PaymentId}. Order COMPLETED.", ctx.Saga.OrderId, ctx.Message.PaymentId);
                })
                .TransitionTo(Completed)
                .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.CustomerEmail,
                    Status = "Completed",
                    Message = $"Your order has been confirmed! Payment: {ctx.Saga.PaymentId}"
                }))
                .Finalize(),

            When(PaymentFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    _logger.LogWarning("[SAGA] Payment FAILED for Order {OrderId}. Reason: {Reason}. Releasing stock.", ctx.Saga.OrderId, ctx.Message.Reason);
                })
                .TransitionTo(Failed)
                // Compensating transaction: release the reserved stock
                .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ReservationId = ctx.Saga.ReservationId!.Value
                }))
                .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.CustomerEmail,
                    Status = "Failed",
                    Message = $"Your order failed during payment: {ctx.Saga.FailureReason}"
                }))
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
