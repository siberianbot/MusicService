using MusicService.Enums;
using MusicService.Models;
using MusicService.Utils;

namespace MusicService.Services;

public class FileActionQueueService
{
    private record FileActionQueueEntry(MediaEntry Entry, FileAction Action);

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Queue<FileActionQueueEntry> _queue = new();
    private readonly HashSet<FileActionQueueEntry> _entries = new();

    public bool IsEmpty => _queue.Count == 0;

    public int Count => _queue.Count;

    public void Enqueue(MediaEntry entry, FileAction action)
    {
        using SemaphoreGuard guard = SemaphoreGuard.Acquire(_semaphore);

        FileActionQueueEntry queueEntry = new(entry, action);

        if (!_entries.Add(queueEntry))
        {
            return;
        }

        _queue.Enqueue(queueEntry);
    }

    public async Task EnqueueAsync(MediaEntry entry, FileAction action, CancellationToken token)
    {
        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        FileActionQueueEntry queueEntry = new(entry, action);

        if (!_entries.Add(queueEntry))
        {
            return;
        }

        _queue.Enqueue(queueEntry);
    }

    public async Task<(MediaEntry entry, FileAction action)?> TryDequeueAsync(CancellationToken token)
    {
        using SemaphoreGuard guard = await SemaphoreGuard.AcquireAsync(_semaphore, token);

        if (!_queue.TryDequeue(out FileActionQueueEntry? queueEntry))
        {
            return null;
        }

        _entries.Remove(queueEntry);

        (MediaEntry entry, FileAction action) = queueEntry;

        return (entry, action);
    }
}