using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;
using Quartz;

namespace ModularMonolithEventDriven.Common.Infrastructure.Jobs;

/// <summary>
/// Quartz job that runs every 10 seconds.
/// Resolves all registered IOutboxMessageProcessor implementations (one per module)
/// and calls ProcessAsync on each, dispatching pending domain events via MediatR.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessOutboxMessagesJob> logger) : IJob
{
    public static readonly JobKey Key = new(nameof(ProcessOutboxMessagesJob), "outbox");

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Outbox job started");

        using var scope = scopeFactory.CreateScope();
        var processors = scope.ServiceProvider.GetRequiredService<IEnumerable<IOutboxMessageProcessor>>();

        foreach (var processor in processors)
        {
            await processor.ProcessAsync(context.CancellationToken);
        }

        logger.LogDebug("Outbox job finished");
    }
}
