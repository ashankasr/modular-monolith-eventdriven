using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Modules.Notifications.Domain.Errors;

public static class NotificationErrors
{
    public static readonly Error InvalidOrderId =
        Error.Validation("Notification.InvalidOrderId", "Order ID cannot be empty.");

    public static readonly Error InvalidRecipientEmail =
        Error.Validation("Notification.InvalidRecipientEmail", "Recipient email cannot be empty.");

    public static readonly Error InvalidStatus =
        Error.Validation("Notification.InvalidStatus", "Notification status cannot be empty.");

    public static readonly Error InvalidMessage =
        Error.Validation("Notification.InvalidMessage", "Notification message cannot be empty.");
}
