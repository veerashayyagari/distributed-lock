namespace DistributedStorage.Lib;


public class DistributedBlobStoreProvider<T> : IDistributedStoreProvider<T> where T : new()
{
    public DistributedBlobStoreProvider(string containerName, string connectionString)
    {

    }

    public Task<bool> TryCommitAsync(Func<T?, CancellationToken, Task<T>> processFileData, CancellationToken cts)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, T?)> TryReadAsync(CancellationToken cts)
    {
        throw new NotImplementedException();
    }
}