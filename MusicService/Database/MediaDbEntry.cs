using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MusicService.Database;

[Index(nameof(SourceFilePath))]
[Index(nameof(TargetFilePath))]
public class MediaDbEntry
{
    [Key]
    [Required]
    public string SourceFilePath { get; set; } = null!;

    [Required]
    public string TargetFilePath { get; set; } = null!;

    [Required]
    public DateTime Timestamp { get; set; }
}