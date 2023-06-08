namespace Api.Models;

public class Status
{
    public string Type { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public Status(string type, DateTime? lastSyncAt)
    {
        Type = type;
        LastSyncAt = lastSyncAt;
    }

    public static Status Healthy(DateTime? lastSyncAt)
    {
        return new Status(StatusType.Healthy, lastSyncAt);
    }

    public static Status UnHealthy(DateTime? lastSyncAt)
    {
        return new Status(StatusType.UnHealthy, lastSyncAt);
    }
}