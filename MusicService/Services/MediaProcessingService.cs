using MusicService.Database;
using MusicService.Enums;
using MusicService.Models;

namespace MusicService.Services;

public class MediaProcessingService(
    ILogger<MediaProcessingService> logger,
    FFMpegService ffMpegService,
    MediaDbService mediaDbService,
    MediaEntryHelper mediaEntryHelper)
{
    public async Task ProcessAsync(MediaEntry entry, FileAction action, CancellationToken token)
    {
        logger.LogInformation("Processing {sourceFilePath}...", entry.AbsoluteSourceFilePath);

        MediaDbEntry? dbEntry = await mediaDbService.TryGetEntryAsync(entry.RelativeSourceFilePath, token);

        try
        {
            switch (action)
            {
                case FileAction.Create:
                    await CreateAsync(entry, dbEntry, token);
                    break;

                case FileAction.Delete:
                    await DeleteAsync(entry, dbEntry, token);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Invalid file action");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process {sourceFilePath}", entry.AbsoluteSourceFilePath);
        }
    }

    private async Task CreateAsync(MediaEntry entry, MediaDbEntry? dbEntry, CancellationToken token)
    {
        if (dbEntry?.Timestamp >= File.GetLastWriteTimeUtc(entry.AbsoluteSourceFilePath))
        {
            logger.LogInformation("Skipped {sourceFilePath} - file is not modified", entry.AbsoluteSourceFilePath);

            return;
        }

        string directory = Path.GetDirectoryName(entry.AbsoluteTargetFilePath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await ffMpegService.ConvertAsync(entry.AbsoluteSourceFilePath, entry.AbsoluteTargetFilePath, token);

        logger.LogInformation("Converted to {targetFilePath}", entry.AbsoluteTargetFilePath);

        await mediaDbService.SetEntryAsync(dbEntry ?? mediaEntryHelper.MakeDbEntry(entry), token);
    }

    private async Task DeleteAsync(MediaEntry entry, MediaDbEntry? dbEntry, CancellationToken token)
    {
        if (!File.Exists(entry.AbsoluteTargetFilePath))
        {
            logger.LogInformation("Skipped {targetFilePath} - already deleted", entry.AbsoluteTargetFilePath);
        }
        else
        {
            File.Delete(entry.AbsoluteTargetFilePath);

            logger.LogInformation("Deleted {targetFilePath}", entry.AbsoluteTargetFilePath);
        }

        await mediaDbService.RemoveEntryAsync(dbEntry, token);
    }
}