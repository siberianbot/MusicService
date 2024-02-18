using Microsoft.Extensions.Options;
using MusicService.Database;
using MusicService.Models;
using MusicService.Options;

namespace MusicService.Services;

public class MediaEntryHelper(
    IOptions<LibraryOptions> libraryOptions,
    IOptions<ConversionOptions> conversionOptions)
{
    public MediaEntry MakeEntry(string path)
    {
        string sourceFilePath = Path.GetRelativePath(libraryOptions.Value.SourceFullPath, path);
        string targetFilePath;
        bool requiresConversion;

        if (conversionOptions.Value.Conversions.TryGetValue(
                Path.GetExtension(sourceFilePath).TrimStart('.'),
                out string? targetExtension))
        {
            string? directory = Path.GetDirectoryName(sourceFilePath);
            string name = Path.GetFileNameWithoutExtension(sourceFilePath) + "." + targetExtension;

            targetFilePath = directory != null
                ? Path.Combine(directory, name)
                : name;

            requiresConversion = true;
        }
        else
        {
            targetFilePath = sourceFilePath;
            requiresConversion = false;
        }

        return new MediaEntry(
            sourceFilePath,
            targetFilePath,
            Path.GetFullPath(path),
            Path.Combine(libraryOptions.Value.TargetFullPath, targetFilePath),
            requiresConversion);
    }

    public MediaEntry MakeEntry(MediaDbEntry dbEntry)
    {
        return new MediaEntry(
            dbEntry.SourceFilePath,
            dbEntry.TargetFilePath,
            Path.Combine(libraryOptions.Value.SourceFullPath, dbEntry.SourceFilePath),
            Path.Combine(libraryOptions.Value.TargetFullPath, dbEntry.TargetFilePath),
            dbEntry.SourceFilePath != dbEntry.TargetFilePath);
    }

    public MediaDbEntry MakeDbEntry(MediaEntry entry)
    {
        return new MediaDbEntry
        {
            SourceFilePath = entry.RelativeSourceFilePath,
            TargetFilePath = entry.RelativeTargetFilePath,
            Timestamp = DateTime.UtcNow
        };
    }
}