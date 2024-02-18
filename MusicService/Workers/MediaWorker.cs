using Microsoft.Extensions.Options;
using MusicService.Enums;
using MusicService.Models;
using MusicService.Options;
using MusicService.Services;

namespace MusicService.Workers;

public class MediaWorker(
    ILogger<MediaWorker> logger,
    IOptions<LibraryOptions> libraryOptions,
    IOptions<WorkerOptions> workerOptions,
    FileActionQueueService fileActionQueueService,
    MediaEntryHelper mediaEntryHelper,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FileSystemWatcher watcher = BuildWatcher();

        logger.LogInformation("Media worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (fileActionQueueService.IsEmpty)
            {
                await Task.Yield();
            }
            else
            {
                logger.LogInformation("There are {count} file(s) to process", fileActionQueueService.Count);

                try
                {
                    await ProcessAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Media worker caught error while processing");
                }
            }

            await Task.Delay(workerOptions.Value.Timeout, stoppingToken);
        }

        logger.LogInformation("Media worker finished at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        MediaProcessingService processingService = scope.ServiceProvider
            .GetRequiredService<MediaProcessingService>();

        MediaDbService dbService = scope.ServiceProvider
            .GetRequiredService<MediaDbService>();

        await Parallel.ForAsync(0, fileActionQueueService.Count, stoppingToken, async (_, token) =>
        {
            (MediaEntry entry, FileAction action)? tuple = await fileActionQueueService.TryDequeueAsync(token);

            if (tuple != null)
            {
                (MediaEntry mediaEntry, FileAction fileAction) = tuple!.Value;

                await processingService.ProcessAsync(mediaEntry, fileAction, token);
            }

            await dbService.FlushAsync(false, token);
        });

        await dbService.FlushAsync(true, stoppingToken);
    }

    private FileSystemWatcher BuildWatcher()
    {
        FileSystemWatcher watcher = new(libraryOptions.Value.SourceFullPath);

        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.Size |
                               NotifyFilters.FileName |
                               NotifyFilters.DirectoryName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime;

        foreach (string type in Constants.Types.AllTypes)
        {
            watcher.Filters.Add($"*.{type}");
        }

        watcher.Created += (_, args) =>
        {
            logger.LogInformation("File created: {file}", args.FullPath);

            fileActionQueueService.Enqueue(mediaEntryHelper.MakeEntry(args.FullPath), FileAction.Create);
        };

        watcher.Renamed += (_, args) =>
        {
            logger.LogInformation("File renamed: {file}", args.FullPath);

            fileActionQueueService.Enqueue(mediaEntryHelper.MakeEntry(args.OldFullPath), FileAction.Delete);
            fileActionQueueService.Enqueue(mediaEntryHelper.MakeEntry(args.FullPath), FileAction.Create);
        };

        watcher.Changed += (_, args) =>
        {
            logger.LogInformation("File modified: {file}", args.FullPath);

            fileActionQueueService.Enqueue(mediaEntryHelper.MakeEntry(args.FullPath), FileAction.Create);
        };

        watcher.Deleted += (_, args) =>
        {
            logger.LogInformation("File deleted: {file}", args.FullPath);

            fileActionQueueService.Enqueue(mediaEntryHelper.MakeEntry(args.FullPath), FileAction.Delete);
        };

        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}