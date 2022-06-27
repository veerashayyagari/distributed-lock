namespace DistributedStorage.Client;

public class ProcessData
{
    public int ProcessId { get; set; }
    public string? HostName { get; set; }
    public long Ticks { get; set; }

    public override string ToString()
    {
        return $"{ProcessId} - {Ticks} - {HostName}";
    }
}