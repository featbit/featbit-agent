namespace Api.Store;

public interface IAgentStore
{
    long Version { get; }

    ValueTask PopulateAsync(DataSet dataSet);

    ValueTask UpdateAsync(StoreItem[] items);

    ValueTask<byte[]> GetDataSyncPayloadAsync();

    ValueTask<DataSetSnapshot> GetSnapshotAsync();

    ValueTask<string[]> GetFlagReferencesAsync(Guid envId, string segmentId);
}