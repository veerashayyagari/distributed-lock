// See https://aka.ms/new-console-template for more information
namespace DistributedStorage.Client;

using DistributedStorage.Lib;

internal class Program
{
    private async static Task Main(string[] args)
    {
        var distributedStoreProvider = GetBlobStoreProvider();

        (bool readSuccessful, ProcessData? data) = await distributedStoreProvider.TryReadAsync(default);
        Console.WriteLine($"{readSuccessful} and {data}");

        bool commitSuccessful = await distributedStoreProvider.TryCommitAsync(ProcessFileData, default);
        Console.WriteLine($"{Environment.ProcessId} - {commitSuccessful}");

        (readSuccessful, data) = await distributedStoreProvider.TryReadAsync(default);
        Console.WriteLine($"{readSuccessful} and {data}");
    }

    private static IDistributedStoreProvider<ProcessData> GetFileStoreProvider()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "distributedfile.txt");
        IDistributedStoreProvider<ProcessData> distributedStoreProvider = new DistributedFileStoreProvider<ProcessData>(filePath);
        return distributedStoreProvider;
    }

    private static IDistributedStoreProvider<ProcessData> GetBlobStoreProvider()
    {
        string blobName = "vulnscan/distributedfile.txt";
        string containerName = "appsecurity";
        string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? string.Empty;
        IDistributedStoreProvider<ProcessData> blobStoreProvider = new DistributedBlobStoreProvider<ProcessData>(blobName, containerName, connectionString);
        return blobStoreProvider;
    }

    private static async Task<ProcessData> ProcessFileData(ProcessData? data, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            ct.ThrowIfCancellationRequested();
        }

        int pid = Environment.ProcessId;
        string hostname = Environment.MachineName;
        long ticks = DateTime.UtcNow.Ticks;
        await Task.Delay(10000, ct);
        data = new ProcessData { HostName = hostname, ProcessId = pid, Ticks = ticks };
        return data;
    }
}