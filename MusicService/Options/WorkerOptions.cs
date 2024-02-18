using System.ComponentModel.DataAnnotations;

namespace MusicService.Options;

public class WorkerOptions
{
    [Required]
    public int Timeout { get; set; } = 1000;
}