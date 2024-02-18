using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;

namespace MusicService.Services;

public class FFMpegService(ILogger<FFMpegService> logger)
{
    public bool IsPresent()
    {
        try
        {
            FFMpegHelper.VerifyFFMpegExists(new FFOptions());

            return true;
        }
        catch (FFMpegException exception) when (exception.Type == FFMpegExceptionType.Operation)
        {
            return false;
        }
    }

    public async Task ConvertAsync(string sourceFilePath, string targetFilePath, CancellationToken token)
    {
        await FFMpegArguments.FromFileInput(sourceFilePath)
            .OutputToFile(targetFilePath, true, arguments => arguments
                .OverwriteExisting()
                .WithAudioBitrate(AudioQuality.Ultra)
                .WithAudioCodec(AudioCodec.LibMp3Lame)
                .WithTagVersion()
                .WithFastStart())
            .WithLogLevel(FFMpegLogLevel.Error)
            .NotifyOnError(ffMpegError => logger.LogError("{ffMpegError}", ffMpegError))
            .CancellableThrough(token)
            .ProcessAsynchronously();
    }
}