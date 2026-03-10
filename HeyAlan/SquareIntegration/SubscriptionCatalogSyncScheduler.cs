namespace HeyAlan.SquareIntegration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class SubscriptionCatalogSyncScheduler : BackgroundService
{
    private static readonly TimeSpan SchedulerTickInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<SubscriptionCatalogSyncScheduler> logger;

    public SubscriptionCatalogSyncScheduler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SubscriptionCatalogSyncScheduler> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.RunTickAsync(stoppingToken);

        using PeriodicTimer timer = new(SchedulerTickInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await this.RunTickAsync(stoppingToken);
        }
    }

    private async Task RunTickAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            ISubscriptionCatalogSyncTriggerService triggerService = scope.ServiceProvider
                .GetRequiredService<ISubscriptionCatalogSyncTriggerService>();

            int enqueuedCount = await triggerService.EnqueueDuePeriodicSyncsAsync(
                DateTime.UtcNow,
                cancellationToken);

            if (enqueuedCount > 0)
            {
                this.logger.LogInformation(
                    "Enqueued {EnqueuedCount} due periodic Square catalog sync request(s).",
                    enqueuedCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Periodic Square catalog sync scheduler tick failed.");
        }
    }
}
