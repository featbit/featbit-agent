using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Messaging;
using Api.Store;
using Domain.Messages;
using Streaming.Protocol;

namespace Api.DataSynchronizer;

public class DataSyncMessageHandler(IAgentStore agentStore, IDataChangeNotifier dataChangeNotifier)
    : IDataSyncMessageHandler
{
    public ValueTask<byte[]> GetRequestPayloadAsync() => agentStore.GetDataSyncPayloadAsync();

    public async Task HandleAsync(JsonElement message)
    {
        var data = message.GetProperty("data");
        var eventType = data.GetProperty("eventType").GetString();

        switch (eventType)
        {
            case DataSyncEventTypes.RpFull:
                await agentStore.PopulateAsync(DataSet.FromJson(data));
                break;
            case DataSyncEventTypes.RpPatch:
            {
                var dataSet = DataSet.FromJson(data);
                var items = dataSet.Items
                    .SelectMany(x => x.FeatureFlags.Concat(x.Segments))
                    .ToArray();
                await agentStore.UpdateAsync(items);

                // notify changes in background
                _ = NotifyItemsUpdated(items);
                break;
            }
            case DataSyncEventTypes.Patch:
            {
                var patchDataSet = PatchDataSet.FromJson(data);
                await agentStore.UpdateAsync(patchDataSet.Items);

                // notify changes in background
                _ = NotifyItemsUpdated(patchDataSet.Items);
                break;
            }
        }
    }

    private async Task NotifyItemsUpdated(StoreItem[] items)
    {
        List<DataChangeMessage> dataChanges = [];

        foreach (var item in items)
        {
            await AddDataChange(item);
        }

        await dataChangeNotifier.NotifyAsync(dataChanges.ToArray());

        return;

        async Task AddDataChange(StoreItem item)
        {
            if (item.Type == StoreItemType.Flag)
            {
                var flagChange = new DataChangeMessage(
                    Topics.FeatureFlagChange,
                    item.Id,
                    Encoding.UTF8.GetString(item.JsonBytes)
                );

                dataChanges.Add(flagChange);
            }

            if (item.Type == StoreItemType.Segment)
            {
                using var segment = JsonDocument.Parse(item.JsonBytes);

                var envId = segment.RootElement.GetProperty("envId").GetGuid();
                var affectedIds = await agentStore.GetFlagReferencesAsync(envId, item.Id);

                JsonObject payload = new()
                {
                    ["segment"] = JsonSerializer.SerializeToNode(segment),
                    ["affectedFlagIds"] = JsonSerializer.SerializeToNode(affectedIds)
                };

                var segmentChange = new DataChangeMessage(
                    Topics.SegmentChange,
                    item.Id,
                    JsonSerializer.Serialize(payload)
                );

                dataChanges.Add(segmentChange);
            }
        }
    }
}