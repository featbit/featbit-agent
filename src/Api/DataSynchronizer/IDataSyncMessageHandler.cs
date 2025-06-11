using System.Text.Json;

namespace Api.DataSynchronizer;

public interface IDataSyncMessageHandler
{
    ValueTask<byte[]> GetRequestPayloadAsync();

    Task HandleAsync(JsonElement message);
}