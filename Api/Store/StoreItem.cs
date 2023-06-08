namespace Api.Store;

public class StoreItem
{
    public string Id { get; set; }

    public Guid EnvId { get; set; }

    public string Type { get; set; }

    public long Timestamp { get; set; }

    public byte[] JsonBytes { get; set; }

    public StoreItem(string id, Guid envId, string type, long timestamp, byte[] jsonBytes)
    {
        Id = id;
        EnvId = envId;
        Type = type;
        Timestamp = timestamp;
        JsonBytes = jsonBytes;
    }
}