namespace MusicService.Utils;

public readonly struct SemaphoreGuard(SemaphoreSlim semaphore) : IDisposable
{
    public void Dispose()
    {
        semaphore.Release();
    }

    public static SemaphoreGuard Acquire(SemaphoreSlim semaphore)
    {
        semaphore.Wait();

        return new SemaphoreGuard(semaphore);
    }

    public static async Task<SemaphoreGuard> AcquireAsync(SemaphoreSlim semaphore, CancellationToken token = default)
    {
        await semaphore.WaitAsync(token);

        return new SemaphoreGuard(semaphore);
    }
}