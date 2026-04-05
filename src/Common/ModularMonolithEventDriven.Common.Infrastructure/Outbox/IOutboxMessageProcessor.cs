namespace ModularMonolithEventDriven.Common.Infrastructure.Outbox;

/// <summary>
/// Each module registers one implementation of this interface (OutboxMessageProcessor&lt;TDbContext&gt;).
/// The Quartz job resolves all registrations and processes each module's outbox in turn.
/// </summary>
public interface IOutboxMessageProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
