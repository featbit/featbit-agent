using System.Text.Json;

namespace Api.Store;

public readonly record struct StoreItemKey(Guid EnvId, string Type, string Identity);

public class StoreItem
{
    public string Id { get; set; }

    public string Identity { get; private set; }

    public Guid EnvId { get; set; }

    public string Type { get; set; }

    public long Timestamp { get; set; }

    public byte[] JsonBytes { get; set; }

    public StoreItem()
    {
        Id = Guid.NewGuid().ToString();
        Identity = Id;
        EnvId = Guid.Empty;
        Type = string.Empty;
        Timestamp = 0;
        JsonBytes = [];
    }

    public StoreItem(string id, string identity, Guid envId, string type, long timestamp, byte[] jsonBytes)
    {
        Id = id;
        Identity = identity;
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

        string identity;
        switch (type)
        {
            case StoreItemType.Segment:
            {
                // Segment keys were introduced in FeatBit 5.2.1.
                var key = json.TryGetProperty("key", out var keyProperty)
                    ? keyProperty.GetString() ?? id
                    : id;

                // Shared segments may have the same key across environments,
                // so we prefix the identity with the segment type to avoid collisions.
                var segmentType = json.GetProperty("type").GetString()!;
                identity = $"{segmentType}_{key}";
                break;
            }
            case StoreItemType.Flag:
            {
                identity = json.GetProperty("key").GetString()!;
                break;
            }
            default:
                throw new InvalidOperationException($"Unsupported StoreItem type: {type}");
        }

        var envId = json.GetProperty("envId").GetGuid();
        var timestamp = json.GetProperty("updatedAt").GetDateTimeOffset().ToUnixTimeMilliseconds();
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(json);

        return new StoreItem(id, identity, envId, type, timestamp, jsonBytes);
    }

    public bool HasSegmentReference(string segmentId)
    {
        if (Type != StoreItemType.Flag)
        {
            throw new InvalidOperationException("This method can only be called on flag items.");
        }

        using var json = JsonDocument.Parse(JsonBytes);
        var root = json.RootElement;

        var ruleConditions = root.GetProperty("rules").EnumerateArray()
            .Select(rule => rule.GetProperty("conditions").EnumerateArray())
            .SelectMany(conditions => conditions);

        foreach (var condition in ruleConditions)
        {
            var property = condition.GetProperty("property").GetString();
            if (SegmentConsts.ConditionProperties.Contains(property) == false)
            {
                continue;
            }

            var value = condition.GetProperty("value").GetString();
            if (value?.Contains(segmentId) == false)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}