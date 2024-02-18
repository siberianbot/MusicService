using Microsoft.EntityFrameworkCore;

namespace MusicService.Database;

public class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<MediaDbEntry> Entries { get; set; } = null!;
}