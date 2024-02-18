using System.Collections.Immutable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicService.Database;
using MusicService.Options;
using MusicService.Services;
using MusicService.Workers;

namespace MusicService;

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHost(args);

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        IOptions<LibraryOptions> libraryOptions = host.Services.GetRequiredService<IOptions<LibraryOptions>>();

        logger.LogInformation("Current source library: {sourceLibrary}", libraryOptions.Value.SourceFullPath);
        logger.LogInformation("Current target library: {targetLibrary}", libraryOptions.Value.TargetFullPath);
        logger.LogInformation("Current target library database: {targetLibraryDatabase}",
            libraryOptions.Value.TargetDatabaseFullPath);

        if (!host.Services.GetRequiredService<FFMpegService>().IsPresent())
        {
            logger.LogCritical("FFMpeg is not installed");

            Environment.Exit(1);
        }

        if (!Directory.Exists(libraryOptions.Value.SourceFullPath))
        {
            logger.LogCritical("Directory {sourceLibrary} does not exist", libraryOptions.Value.SourceFullPath);

            Environment.Exit(1);
        }

        if (!Directory.Exists(libraryOptions.Value.TargetFullPath))
        {
            Directory.CreateDirectory(libraryOptions.Value.TargetFullPath);
        }

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
        }

        await host.RunAsync();
    }

    private static IHost CreateHost(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddOptions<WorkerOptions>()
            .BindConfiguration("Worker")
            .ValidateOnStart();

        builder.Services.AddOptions<LibraryOptions>()
            .BindConfiguration("Library")
            .ValidateOnStart();

        builder.Services.AddOptions<ConversionOptions>()
            .Configure((ConversionOptions options, IConfiguration configuration) =>
            {
                options.Conversions = configuration.GetSection("Conversions")
                    .GetChildren()
                    .ToImmutableDictionary(x => x.Key,
                        x => x.Value ??
                             throw new ArgumentNullException(x.Key, $"Conversion for {x.Key} defined as null"));
            });

        builder.Services.AddDbContext<MediaDbContext>((provider, options) =>
        {
            LibraryOptions? libraryOptions = provider.GetService<IOptions<LibraryOptions>>()?.Value;

            SqliteConnectionStringBuilder connectionStringBuilder = new()
            {
                DataSource = libraryOptions?.TargetDatabaseFullPath ?? Constants.Files.DatabaseFileName
            };

            options.UseSqlite(connectionStringBuilder.ConnectionString);
        });

        builder.Services.AddSingleton<FFMpegService>();
        builder.Services.AddSingleton<FileActionQueueService>();
        builder.Services.AddSingleton<MediaEntryHelper>();

        builder.Services.AddScoped<MediaDbService>();
        builder.Services.AddScoped<MediaProcessingService>();
        builder.Services.AddScoped<MediaSynchronizationService>();

        builder.Services.AddHostedService<MediaWorker>();
        builder.Services.AddHostedService<SynchronizationWorker>();

        return builder.Build();
    }
}