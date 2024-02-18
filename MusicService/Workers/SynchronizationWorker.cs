using MusicService.Services;

namespace MusicService.Workers;

public class SynchronizationWorker(
    ILogger<SynchronizationWorker> logger,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Synchronization worker started at: {time}", DateTimeOffset.Now);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MediaSynchronizationService>().SyncAsync(stoppingToken);
        }

        logger.LogInformation("Synchronization worker finished at: {time}", DateTimeOffset.Now);
    }
}