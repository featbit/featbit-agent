using System.Text.Json;

namespace Api.Store;

public class PatchDataSet
{
    public StoreItem[] Items { get; set; } = [];

    public static PatchDataSet FromJson(JsonElement json)
    {
        var flags = json.GetProperty("featureFlags").EnumerateArray()
            .Select(flag => StoreItem.Of(flag, StoreItemType.Flag));

        var segments = json.GetProperty("segments").EnumerateArray()
            .Select(segment => StoreItem.Of(segment, StoreItemType.Segment));

        var items = flags.Concat(segments).ToArray();
        return new PatchDataSet
        {
            Items = items
        };
    }
}