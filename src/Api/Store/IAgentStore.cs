namespace Api.Store;

public interface IAgentStore
{
    long Version { get; }

    string Serves { get; }

    string AgentId { get; }

    ValueTask SaveAgentIdAsync(string agentId);

    ValueTask PopulateAsync(DataSet dataSet);

    ValueTask UpdateAsync(StoreItem[] items);

    ValueTask<byte[]> GetDataSyncPayloadAsync();

    ValueTask<DataSetSnapshot> GetSnapshotAsync();

    ValueTask<string[]> GetFlagReferencesAsync(Guid envId, string segmentId);
}