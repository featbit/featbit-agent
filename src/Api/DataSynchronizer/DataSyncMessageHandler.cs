using System.Text.Json;
using Api.Messaging;
using Api.Store;
using Streaming.Protocol;

namespace Api.DataSynchronizer;

public class DataSyncMessageHandler(
    IAgentStore agentStore,
    IDataChangeNotifier dataChangeNotifier,
    ILogger<DataSyncMessageHandler> logger)
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

                await SafeNotifyAsync(items);
                break;
            }
            case DataSyncEventTypes.Patch:
            {
                var patchDataSet = PatchDataSet.FromJson(data);
                await agentStore.UpdateAsync(patchDataSet.Items);

                await SafeNotifyAsync(patchDataSet.Items);
                break;
            }
        }
    }

    private async Task SafeNotifyAsync(StoreItem[] items)
    {
        foreach (var item in items)
        {
            try
            {
                await dataChangeNotifier.NotifyAsync(item);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Exception occurred while processing StoreItem Id {Id} of type {Type} for data change notification.",
                    item.Id,
                    item.Type
                );
            }
        }
    }
}