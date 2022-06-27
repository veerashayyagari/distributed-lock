namespace DistributedStorage.Lib;

using System.IO;
using Newtonsoft.Json;

public class DistributedFileStoreProvider<T> : IDistributedStoreProvider<T> where T : new()
{
    private readonly string fileStorePath;
    public DistributedFileStoreProvider(string filePath)
    {
        this.fileStorePath = filePath;
        Initialize();
    }

    public async Task<bool> TryCommitAsync(Func<T?, CancellationToken, Task<T>> processFile, CancellationToken ct)
    {
        bool commitSuccessful = false;

        try
        {
            using (FileStream fs = File.Open(this.fileStorePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                // long length = fs.Length;
                // fs.Lock(0, length);
                T? currentData = await ReadDataFromStream(fs, ct);

                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource(15000);
                    currentData = await processFile(currentData, cts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error Running Process File Action {e}");
                    return commitSuccessful;
                }

                string dataToWrite = JsonConvert.SerializeObject(currentData);
                byte[] bytesToWrite = System.Text.UnicodeEncoding.Default.GetBytes(dataToWrite);
                fs.Seek(0, SeekOrigin.Begin);
                await fs.WriteAsync(bytesToWrite, 0, bytesToWrite.Length, ct);
                commitSuccessful = true;
                //fs.Unlock(0, length);
            }
        }
        catch (IOException ioex)
        {
            Console.WriteLine($"IO Exception , {ioex.HResult} : {ioex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Exception: {ex} ");
        }

        return commitSuccessful;
    }

    public async Task<(bool, T?)> TryReadAsync(CancellationToken cts)
    {
        T? readData = default(T);
        bool readSuccessful = false;

        try
        {
            using (FileStream fs = File.Open(this.fileStorePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            {
                readData = await ReadDataFromStream(fs, cts);
                await Task.Delay(10000);
                readSuccessful = true;
            }
        }
        catch (IOException ioex)
        {
            Console.WriteLine($"{ioex.HResult} : {ioex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown Exception : {ex}");
        }

        return (readSuccessful, readData);
    }

    private async Task<T?> ReadDataFromStream(FileStream fs, CancellationToken cts)
    {
        // brand new file, just return
        if (fs.Length == 0)
        {
            return default(T);
        }
        else
        {
            byte[] buffer = new byte[fs.Length];
            fs.Seek(0, SeekOrigin.Begin);
            await fs.ReadAsync(buffer, 0, buffer.Length, cts);
            string bufferedData = System.Text.UnicodeEncoding.Default.GetString(buffer, 0, buffer.Length);
            return JsonConvert.DeserializeObject<T>(bufferedData);
        }
    }

    private void Initialize()
    {
        string? fileDirectory = Path.GetDirectoryName(this.fileStorePath);
        if (!string.IsNullOrEmpty(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }
    }
}