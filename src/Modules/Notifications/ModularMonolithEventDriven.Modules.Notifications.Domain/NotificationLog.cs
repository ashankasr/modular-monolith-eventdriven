using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Notifications.Domain.Errors;

namespace ModularMonolithEventDriven.Modules.Notifications.Domain;

public sealed class NotificationLog : AuditableGuidEntity
{
    private NotificationLog() { }

    private NotificationLog(Guid id, Guid orderId, string recipientEmail, string status, string message) : base(id)
    {
        OrderId = orderId;
        RecipientEmail = recipientEmail;
        Status = status;
        Message = message;
    }

    public Guid OrderId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public static Result<NotificationLog> Create(Guid id, Guid orderId, string recipientEmail, string status, string message)
    {
        if (orderId == Guid.Empty)
            return Result.Failure<NotificationLog>(NotificationErrors.InvalidOrderId);

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return Result.Failure<NotificationLog>(NotificationErrors.InvalidRecipientEmail);

        if (string.IsNullOrWhiteSpace(status))
            return Result.Failure<NotificationLog>(NotificationErrors.InvalidStatus);

        if (string.IsNullOrWhiteSpace(message))
            return Result.Failure<NotificationLog>(NotificationErrors.InvalidMessage);

        return new NotificationLog(id, orderId, recipientEmail, status, message);
    }
}
