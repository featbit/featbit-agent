using System.Text.Json;
using Domain.Shared;
using Streaming.Protocol;

namespace Api.Store;

public class DataSet
{
    public string EventType { get; set; } = DataSyncEventTypes.RpFull;

    public DataSetItem[] Items { get; set; } = [];

    public static DataSet FromJson(JsonElement json)
    {
        var eventType = json.GetProperty("eventType").GetString()!;

        var items = new List<DataSetItem>();

        var itemsProperty = json.GetProperty("items");
        foreach (var item in itemsProperty.EnumerateArray())
        {
            var envId = item.GetProperty("envId").GetGuid();

            SecretWithValue[] secrets = [];
            if (item.TryGetProperty("secrets", out var secretsProperty) &&
                secretsProperty.ValueKind == JsonValueKind.Array)
            {
                secrets = secretsProperty.Deserialize<SecretWithValue[]>(ReusableJsonSerializerOptions.Web)!;
            }

            var flags = item.GetProperty("featureFlags").EnumerateArray()
                .Select(flag => StoreItem.Of(flag, StoreItemType.Flag))
                .ToArray();

            var segments = item.GetProperty("segments").EnumerateArray()
                .Select(segment => StoreItem.Of(segment, StoreItemType.Segment))
                .ToArray();

            items.Add(new DataSetItem(envId, secrets, flags, segments));
        }

        var dataSet = new DataSet
        {
            EventType = eventType,
            Items = items.ToArray()
        };

        return dataSet;
    }
}

public record DataSetItem(
    Guid EnvId,
    SecretWithValue[] Secrets,
    StoreItem[] FeatureFlags,
    StoreItem[] Segments
);