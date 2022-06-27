namespace DistributedStorage.Lib;
public interface IDistributedStoreProvider<T> where T : new()
{
    Task<(bool, T?)> TryReadAsync(CancellationToken cts);

    Task<bool> TryCommitAsync(Func<T?, CancellationToken, Task<T>> processFileData, CancellationToken cts);
}
