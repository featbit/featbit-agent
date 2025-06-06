using System.Text.Json;
using System.Text.Json.Nodes;
using Domain.Shared;
using Streaming.Protocol;

namespace Api.Store;

internal sealed class InMemoryStore : IAgentStore, IStore
{
    private static readonly byte[] FullDataSync =
        "{\"messageType\":\"data-sync\",\"data\":{\"timestamp\":0}}"u8.ToArray();

    private readonly object _writeLock = new();

    private readonly Dictionary<Guid, long> _envVersions = new();
    private readonly List<SecretWithValue> _secrets = [];
    private readonly List<StoreItem> _items = [];

    public string Name => "Memory";
    public long Version { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ValueTask PopulateAsync(DataSet dataSet)
    {
        lock (_writeLock)
        {
            _secrets.Clear();
            _items.Clear();

            foreach (var item in dataSet.Items)
            {
                _secrets.AddRange(item.Secrets);

                _items.AddRange(item.FeatureFlags);
                _items.AddRange(item.Segments);

                // update versions
                var envVersion = item.FeatureFlags.Select(x => x.Timestamp)
                    .Concat(item.Segments.Select(x => x.Timestamp))
                    .Max();

                _envVersions[item.EnvId] = envVersion;
                if (envVersion > Version)
                {
                    Version = envVersion;
                }
            }

            UpdatedAt = DateTime.UtcNow;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(StoreItem[] items)
    {
        lock (_writeLock)
        {
            foreach (var item in items)
            {
                UpdateItem(item);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        return ValueTask.CompletedTask;

        void UpdateItem(StoreItem item)
        {
            var existingItem = _items.FirstOrDefault(x => x.Id == item.Id);
            if (existingItem != null && existingItem.Timestamp < item.Timestamp)
            {
                existingItem.Update(item);
            }
            else
            {
                _items.Add(item);
            }

            // update versions
            if (item.Timestamp > Version)
            {
                Version = item.Timestamp;
            }

            _envVersions[item.EnvId] = Math.Max(_envVersions.GetValueOrDefault(item.EnvId, 0), item.Timestamp);
        }
    }

    public ValueTask<byte[]> GetDataSyncPayloadAsync()
    {
        if (Version == 0)
        {
            return ValueTask.FromResult(FullDataSync);
        }

        object patchDataSync = new
        {
            messageType = "data-sync",
            data = new
            {
                timestamp = Version,
                envs = _envVersions.Select(x => new
                {
                    envId = x.Key,
                    timestamp = x.Value
                })
            }
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(patchDataSync);
        return ValueTask.FromResult(bytes);
    }

    public ValueTask<DataSetSnapshot> GetSnapshotAsync()
    {
        lock (_writeLock)
        {
            var items = new List<DataSetSnapshotItem>();

            var groupedSecrets = _secrets.GroupBy(x => x.EnvId);
            foreach (var group in groupedSecrets)
            {
                var envId = group.Key;
                var secrets = group.ToArray();

                var flags = _items
                    .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag)
                    .Select(x => JsonSerializer.Deserialize<JsonObject>(x.JsonBytes)!)
                    .ToArray();

                var segments = _items
                    .Where(x => x.EnvId == envId && x.Type == StoreItemType.Segment)
                    .Select(x => JsonSerializer.Deserialize<JsonObject>(x.JsonBytes)!)
                    .ToArray();

                var dataSetItem = new DataSetSnapshotItem(envId, secrets, flags, segments);
                items.Add(dataSetItem);
            }

            var dataSet = new DataSetSnapshot(DataSyncEventTypes.RpFull, items.ToArray());

            return ValueTask.FromResult(dataSet);
        }
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    public Task<IEnumerable<byte[]>> GetFlagsAsync(Guid envId, long timestamp)
    {
        var flags = _items
            .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag && x.Timestamp > timestamp)
            .Select(x => x.JsonBytes);

        return Task.FromResult(flags);
    }

    public Task<IEnumerable<byte[]>> GetFlagsAsync(IEnumerable<string> ids)
    {
        var flags = _items
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.JsonBytes);

        return Task.FromResult(flags);
    }

    public Task<byte[]> GetSegmentAsync(string id)
    {
        var segment = _items.First(x => x.Id == id).JsonBytes;
        return Task.FromResult(segment);
    }

    public Task<IEnumerable<byte[]>> GetSegmentsAsync(Guid envId, long timestamp)
    {
        var segments = _items
            .Where(x => x.EnvId == envId && x.Type == StoreItemType.Segment && x.Timestamp > timestamp)
            .Select(x => x.JsonBytes);

        return Task.FromResult(segments);
    }

    public Task<Secret?> GetSecretAsync(string secretString)
    {
        var secretWithValue = _secrets.FirstOrDefault(x => x.Value == secretString);

        var secret = secretWithValue?.AsSecret();
        return Task.FromResult(secret);
    }
}