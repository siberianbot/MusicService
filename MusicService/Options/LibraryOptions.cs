using System.ComponentModel.DataAnnotations;

namespace MusicService.Options;

public class LibraryOptions
{
    [Required]
    public string Source { get; set; } = null!;

    [Required]
    public string Target { get; set; } = null!;

    public string SourceFullPath => Path.GetFullPath(Source);

    public string TargetFullPath => Path.GetFullPath(Target);

    public string TargetDatabaseFullPath => Path.Combine(TargetFullPath, Constants.Files.DatabaseFileName);
}