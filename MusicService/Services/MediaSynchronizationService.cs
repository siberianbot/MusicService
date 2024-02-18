using Microsoft.Extensions.Options;
using MusicService.Database;
using MusicService.Enums;
using MusicService.Options;
using MusicService.Utils;

namespace MusicService.Services;

public class MediaSynchronizationService(
    ILogger<MediaSynchronizationService> logger,
    IOptions<LibraryOptions> libraryOptions,
    FileActionQueueService fileActionQueueService,
    MediaDbContext mediaDbContext,
    MediaDbService mediaDbService,
    MediaEntryHelper mediaEntryHelper)
{
    public async Task SyncAsync(CancellationToken token)
    {
        logger.LogInformation("Synchronizing media library");

        try
        {
            logger.LogInformation("Synchronizing database with library files...");

            await SyncDatabaseAsync(token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to synchronize database with library files");
        }

        try
        {
            logger.LogInformation("Synchronizing library files to database...");

            await SyncLibraryAsync(token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to synchronize library files to database");
        }
    }

    private async Task SyncDatabaseAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(mediaDbContext.Entries.AsAsyncEnumerable(), stoppingToken, async (dbEntry, token) =>
        {
            logger.LogDebug("Processing {sourceFilePath}...", libraryOptions.Value.SourceFullPath);

            string sourceFullPath = Path.Combine(libraryOptions.Value.SourceFullPath, dbEntry.SourceFilePath);

            if (File.Exists(sourceFullPath))
            {
                return;
            }

            logger.LogInformation(
                "File {targetFilePath} is going to be deleted - file {sourceFilePath} not found in source library",
                dbEntry.TargetFilePath, dbEntry.SourceFilePath
            );

            await fileActionQueueService.EnqueueAsync(mediaEntryHelper.MakeEntry(dbEntry), FileAction.Delete, token);
        });
    }

    private async Task SyncLibraryAsync(CancellationToken stoppingToken)
    {
        IEnumerable<string> files = DirectoryLookupUtils.GetFilesRecursive(libraryOptions.Value.SourceFullPath,
            Constants.Types.AllTypes);

        await Parallel.ForEachAsync(files, stoppingToken, async (file, token) =>
        {
            logger.LogDebug("Processing {file}...", file);

            try
            {
                await ProcessFileAsync(file, token);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process file {file}", file);
            }
        });
    }

    private async Task ProcessFileAsync(string file, CancellationToken token)
    {
        string relativeSourceFilePath = Path.GetRelativePath(libraryOptions.Value.SourceFullPath, file);

        MediaDbEntry? dbEntry = await mediaDbService.TryGetEntryAsync(relativeSourceFilePath, token);

        Lazy<string> absoluteTargetFilePath = new(() => Path.Combine(libraryOptions.Value.TargetFullPath,
            dbEntry!.TargetFilePath));

        if (dbEntry == null)
        {
            logger.LogInformation("File {sourceFilePath} is going to be created - not presented in database",
                relativeSourceFilePath);

            await fileActionQueueService.EnqueueAsync(mediaEntryHelper.MakeEntry(file), FileAction.Create, token);
        }
        else if (!File.Exists(absoluteTargetFilePath.Value))
        {
            logger.LogInformation("File {sourceFilePath} is going to be created - target {targetFilePath} is missing",
                relativeSourceFilePath, dbEntry.TargetFilePath);

            await fileActionQueueService.EnqueueAsync(mediaEntryHelper.MakeEntry(dbEntry), FileAction.Create, token);
        }
        else if (dbEntry.Timestamp < File.GetLastWriteTimeUtc(relativeSourceFilePath))
        {
            logger.LogInformation("File {sourceFilePath} is going to be created - file is modified",
                relativeSourceFilePath);

            await fileActionQueueService.EnqueueAsync(mediaEntryHelper.MakeEntry(dbEntry), FileAction.Create, token);
        }
    }
}