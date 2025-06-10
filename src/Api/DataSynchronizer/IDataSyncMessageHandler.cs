using System.Text.Json;

namespace Api.DataSynchronizer;

public interface IDataSyncMessageHandler
{
    ValueTask<byte[]> GetPayloadAsync();

    Task HandleAsync(JsonElement message);
}