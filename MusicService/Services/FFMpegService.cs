using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;
using Microsoft.Extensions.Options;
using MusicService.Options;

namespace MusicService.Services;

public class FFMpegService(
    ILogger<FFMpegService> logger,
    IOptions<FFMpegOptions> options)
{
    public bool IsPresent()
    {
        try
        {
            FFOptions ffMpegOptions = new()
            {
                BinaryFolder = options.Value.Binaries
            };

            FFMpegHelper.VerifyFFMpegExists(ffMpegOptions);

            GlobalFFOptions.Configure(ffMpegOptions);

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
                .DisableChannel(Channel.Video)
                .CopyChannel(Channel.Audio)
                .WithAudioCodec(AudioCodec.LibFdk_Aac)
                .WithVariableBitrate(5)
                .WithTagVersion()
                .WithFastStart())
            .WithLogLevel(FFMpegLogLevel.Error)
            .NotifyOnError(ffMpegError => logger.LogError("{ffMpegError}", ffMpegError))
            .CancellableThrough(token).ProcessAsynchronously();
    }
}