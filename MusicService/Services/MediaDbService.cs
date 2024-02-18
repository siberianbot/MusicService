using Microsoft.EntityFrameworkCore;
using MusicService.Database;
using MusicService.Utils;

namespace MusicService.Services;

public class MediaDbService(MediaDbContext mediaDbContext)
{
    private enum CacheState
    {
        None,
        Added,
        Updated,
        Deleted
    }

    private record CacheEntry(CacheState State, MediaDbEntry DbEntry)
    {
        public CacheState State { get; set; } = State;
        public MediaDbEntry DbEntry { get; set; } = DbEntry;
    }

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _cache = new();

    public async Task<MediaDbEntry?> TryGetEntryAsync(string sourceFilePath, CancellationToken token)
    {
        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        if (_cache.TryGetValue(sourceFilePath, out CacheEntry? cacheEntry))
        {
            return cacheEntry.DbEntry;
        }

        MediaDbEntry? dbEntry = await mediaDbContext.Entries.FindAsync([sourceFilePath], cancellationToken: token);

        if (dbEntry != null)
        {
            _cache.Add(sourceFilePath, new CacheEntry(CacheState.None, dbEntry));
        }

        return dbEntry;
    }

    public async Task SetEntryAsync(MediaDbEntry dbEntry, CancellationToken token)
    {
        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        if (_cache.TryGetValue(dbEntry.SourceFilePath, out CacheEntry? cacheEntry))
        {
            cacheEntry.DbEntry = dbEntry;
        }
        else
        {
            CacheState targetState = await mediaDbContext.Entries
                .AnyAsync(x => x.SourceFilePath == dbEntry.SourceFilePath, token)
                ? CacheState.Updated
                : CacheState.Added;

            _cache[dbEntry.SourceFilePath] = new CacheEntry(targetState, dbEntry);
        }
    }

    public async Task RemoveEntryAsync(MediaDbEntry? dbEntry, CancellationToken token)
    {
        if (dbEntry == null)
        {
            return;
        }

        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        _cache[dbEntry.SourceFilePath] = new CacheEntry(CacheState.Deleted, dbEntry);
    }

    public async Task FlushAsync(bool dropCache, CancellationToken token)
    {
        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        List<string> forDeletion = [];

        foreach ((string key, CacheEntry cacheEntry) in _cache)
        {
            switch (cacheEntry.State)
            {
                case CacheState.None:
                    // skip
                    break;

                case CacheState.Added:
                    await mediaDbContext.Entries.AddAsync(cacheEntry.DbEntry, token);
                    cacheEntry.State = CacheState.None;
                    break;

                case CacheState.Updated:
                    mediaDbContext.Entries.Update(cacheEntry.DbEntry);
                    cacheEntry.State = CacheState.None;
                    break;

                case CacheState.Deleted:
                    mediaDbContext.Entries.Remove(cacheEntry.DbEntry);
                    forDeletion.Add(key);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheEntry.State), cacheEntry.State,
                        "Invalid cache entry state");
            }
        }

        await mediaDbContext.SaveChangesAsync(token);

        if (dropCache)
        {
            _cache.Clear();
        }
        else
        {
            foreach (string key in forDeletion)
            {
                _cache.Remove(key);
            }
        }
    }
}