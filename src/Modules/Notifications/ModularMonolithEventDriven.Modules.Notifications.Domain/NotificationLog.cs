using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Notifications.Domain;

public sealed class NotificationLog : AuditableGuidEntity
{
    private NotificationLog() { }

    public NotificationLog(Guid id, Guid orderId, string recipientEmail, string status, string message) : base(id)
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
}
