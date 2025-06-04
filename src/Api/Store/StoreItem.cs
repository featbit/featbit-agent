using System.Text.Json;

namespace Api.Store;

public class StoreItem
{
    public string Id { get; set; }

    public Guid EnvId { get; set; }

    public string Type { get; set; }

    public long Timestamp { get; set; }

    public byte[] JsonBytes { get; set; }

    public StoreItem()
    {
        Id = Guid.NewGuid().ToString();
        EnvId = Guid.Empty;
        Type = string.Empty;
        Timestamp = 0;
        JsonBytes = [];
    }

    public StoreItem(string id, Guid envId, string type, long timestamp, byte[] jsonBytes)
    {
        Id = id;
        EnvId = envId;
        Type = type;
        Timestamp = timestamp;
        JsonBytes = jsonBytes;
    }

    public void Update(StoreItem item)
    {
        Timestamp = item.Timestamp;
        JsonBytes = item.JsonBytes;
    }

    public static StoreItem Of(JsonElement json, string type)
    {
        var id = json.GetProperty("id").GetString()!;
        var envId = json.GetProperty("envId").GetGuid();
        var timestamp = json.GetProperty("updatedAt").GetDateTimeOffset().ToUnixTimeMilliseconds();
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(json);

        return new StoreItem(id, envId, type, timestamp, jsonBytes);
    }
}