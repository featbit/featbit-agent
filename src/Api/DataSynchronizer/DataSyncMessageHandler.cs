using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Messaging;
using Api.Store;
using Domain.Messages;
using Streaming.Protocol;

namespace Api.DataSynchronizer;

public class DataSyncMessageHandler(IAgentStore agentAgentStore, IDataChangeNotifier dataChangeNotifier)
    : IDataSyncMessageHandler
{
    public ValueTask<byte[]> GetPayloadAsync() => agentAgentStore.GetDataSyncPayloadAsync();

    public async Task HandleAsync(JsonElement message)
    {
        var data = message.GetProperty("data");
        var eventType = data.GetProperty("eventType").GetString();

        switch (eventType)
        {
            case DataSyncEventTypes.RpFull:
                await agentAgentStore.PopulateAsync(DataSet.FromJson(data));
                break;
            case DataSyncEventTypes.RpPatch:
            {
                var dataSet = DataSet.FromJson(data);
                var items = dataSet.Items
                    .SelectMany(x => x.FeatureFlags.Concat(x.Segments))
                    .ToArray();
                await agentAgentStore.UpdateAsync(items);

                _ = OnItemsUpdated(items);
                break;
            }
            case DataSyncEventTypes.Patch:
            {
                var patchDataSet = PatchDataSet.FromJson(data);
                await agentAgentStore.UpdateAsync(patchDataSet.Items);

                _ = OnItemsUpdated(patchDataSet.Items);
                break;
            }
        }
    }

    private async Task OnItemsUpdated(StoreItem[] items)
    {
        List<DataChangeMessage> dataChangeMessages = [];

        foreach (var item in items)
        {
            if (item.Type == StoreItemType.Flag)
            {
                var flagChangeMessage = new DataChangeMessage(
                    Topics.FeatureFlagChange,
                    item.Id,
                    Encoding.UTF8.GetString(item.JsonBytes)
                );

                dataChangeMessages.Add(flagChangeMessage);
            }
            else if (item.Type == StoreItemType.Segment)
            {
                using var segment = JsonDocument.Parse(item.JsonBytes);

                var envId = segment.RootElement.GetProperty("envId").GetGuid();
                var affectedIds = await agentAgentStore.GetFlagReferencesAsync(envId, item.Id);

                JsonObject segmentChange = new()
                {
                    ["segment"] = JsonSerializer.SerializeToNode(segment),
                    ["affectedFlagIds"] = JsonSerializer.SerializeToNode(affectedIds)
                };

                var segmentChangeMessage = new DataChangeMessage(
                    Topics.SegmentChange,
                    item.Id,
                    JsonSerializer.Serialize(segmentChange)
                );

                dataChangeMessages.Add(segmentChangeMessage);
            }
        }

        await dataChangeNotifier.NotifyAsync(dataChangeMessages.ToArray());
    }
}