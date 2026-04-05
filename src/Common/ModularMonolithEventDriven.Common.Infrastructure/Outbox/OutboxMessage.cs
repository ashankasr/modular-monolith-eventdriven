namespace ModularMonolithEventDriven.Common.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Assembly-qualified type name — used to deserialize Content back to the correct IDomainEvent.</summary>
    public required string Type { get; init; }

    /// <summary>System.Text.Json-serialized domain event payload.</summary>
    public required string Content { get; init; }

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Set by the processor once the event has been dispatched. Null = not yet processed.</summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>Stores the exception message when dispatch fails.</summary>
    public string? Error { get; set; }
}
