using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;

namespace DistributedStorage.Lib;


public class DistributedBlobStoreProvider<T> : IDistributedStoreProvider<T> where T : new()
{
    private readonly BlobContainerClient blobContainerClient;
    private readonly BlobClient blobClient;
    private readonly string blobName;

    public DistributedBlobStoreProvider(string blobName, string containerName, string connectionString)
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
        this.blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        this.blobName = blobName;
        this.blobClient = this.blobContainerClient.GetBlobClient(this.blobName);
        Initialize();
    }

    public async Task<bool> TryCommitAsync(Func<T?, CancellationToken, Task<T>> processFileData, CancellationToken ct)
    {
        bool commitSuccessful = false;
        BlobLeaseClient blobLeaseClient = this.blobClient.GetBlobLeaseClient();
        Response<BlobLease>? leaseInfo = default;

        try
        {
            TimeSpan ts = new TimeSpan(0, 0, 15);
            leaseInfo = await blobLeaseClient.AcquireAsync(ts, cancellationToken: ct);
            Console.WriteLine($"Acquired Lease {leaseInfo.Value.LeaseId} - {leaseInfo.Value.LastModified}");
            T? result = await ReadFromBlob();
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct,
             (new CancellationTokenSource(TimeSpan.FromSeconds(11)).Token));
            result = await processFileData(result, cts.Token);
            BlobUploadOptions options = new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    LeaseId = leaseInfo.Value.LeaseId
                }
            };

            string processedData = JsonConvert.SerializeObject(result);
            await this.blobClient.UploadAsync(new BinaryData(processedData), options, ct);
            commitSuccessful = true;
        }
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.Conflict && rfe.ErrorCode == "LeaseAlreadyPresent")
        {
            Console.WriteLine($"Possible Lease acquired by other client {rfe}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating blob {ex}");
        }
        finally
        {
            if (leaseInfo != null && leaseInfo.Value != null)
            {
                await blobLeaseClient.ReleaseAsync();
            }
        }

        return commitSuccessful;
    }

    public async Task<(bool, T?)> TryReadAsync(CancellationToken cts)
    {
        bool readSuccessful = false;
        T? result = default;

        try
        {
            result = await ReadFromBlob();

            readSuccessful = true;
        }
        catch (JsonReaderException jre)
        {
            Console.WriteLine($"Malformed Json Possible, Writes will overwrite. ${jre}");
            readSuccessful = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading Blob {ex}");
            readSuccessful = false;
        }

        return (readSuccessful, result);
    }

    private async Task<T?> ReadFromBlob()
    {
        T? result = default;
        var response = await this.blobClient.DownloadContentAsync();
        if (response.Value.Details.ContentLength != 0)
        {
            result = response.Value.Content.ToObjectFromJson<T>();
        }

        return result;
    }

    private void Initialize()
    {
        try
        {
            this.blobContainerClient.CreateIfNotExists();
            if (!this.blobClient.Exists())
            {
                this.blobClient.Upload(new BinaryData(string.Empty));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Initializing container and blob {ex}");
        }
    }
}