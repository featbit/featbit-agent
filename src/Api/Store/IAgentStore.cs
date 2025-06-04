namespace Api.Store;

public interface IAgentStore
{
    long Version { get; }

    DateTime? UpdatedAt { get; }

    ValueTask PopulateAsync(DataSet dataSet);

    ValueTask UpdateAsync(StoreItem[] items);

    ValueTask<byte[]> GetDataSyncPayloadAsync();

    ValueTask<DataSetSnapshot> GetSnapshotAsync();
}