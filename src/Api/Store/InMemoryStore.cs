using System.Text.Json;
using System.Text.Json.Nodes;
using Domain.Shared;
using Streaming.Protocol;

namespace Api.Store;

internal sealed class InMemoryStore : IAgentStore, IStore
{
    private static readonly byte[] FullDataSync =
        "{\"messageType\":\"data-sync\",\"data\":{\"timestamp\":0}}"u8.ToArray();


    private readonly Dictionary<Guid, long> _envVersions = new();
    private readonly List<SecretWithValue> _secrets = [];

    private readonly ReaderWriterLockSlim _storeLock = new();
    private readonly Dictionary<StoreItemKey, StoreItem> _items = [];

    public string Name => "Memory";

    public bool Initialized { get; private set; }

    public long Version { get; private set; }

    public string Serves
    {
        get
        {
            _storeLock.EnterReadLock();

            try
            {
                var envs = _secrets
                    .GroupBy(x => x.EnvId)
                    .Select(envSecrets => envSecrets.First())
                    .Select(envSecret => $"{envSecret.ProjectKey}:{envSecret.EnvKey}");

                var served = string.Join(",", envs);
                return served;
            }
            finally
            {
                _storeLock.ExitReadLock();
            }
        }
    }

    public string AgentId { get; private set; } = string.Empty;

    public ValueTask SaveAgentIdAsync(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty.", nameof(agentId));
        }

        AgentId = agentId;

        return ValueTask.CompletedTask;
    }

    public ValueTask PopulateAsync(DataSet dataSet)
    {
        _storeLock.EnterWriteLock();

        try
        {
            _secrets.Clear();
            _items.Clear();

            foreach (var item in dataSet.Items)
            {
                _secrets.AddRange(item.Secrets);

                foreach (var flag in item.FeatureFlags)
                {
                    UpsertItem(flag);
                }

                foreach (var segment in item.Segments)
                {
                    UpsertItem(segment);
                }

                // update versions
                var timestamps = item.FeatureFlags.Select(x => x.Timestamp)
                    .Concat(item.Segments.Select(x => x.Timestamp))
                    .ToArray();

                var envVersion = timestamps.Length != 0 ? timestamps.Max() : 0;

                _envVersions[item.EnvId] = envVersion;
                if (envVersion > Version)
                {
                    Version = envVersion;
                }
            }

            Initialized = true;
        }
        finally
        {
            _storeLock.ExitWriteLock();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(StoreItem[] items)
    {
        _storeLock.EnterWriteLock();

        try
        {
            foreach (var item in items)
            {
                UpsertItem(item);

                // update versions
                if (item.Timestamp > Version)
                {
                    Version = item.Timestamp;
                }

                _envVersions[item.EnvId] = Math.Max(_envVersions.GetValueOrDefault(item.EnvId, 0), item.Timestamp);
            }
        }
        finally
        {
            _storeLock.ExitWriteLock();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<byte[]> GetDataSyncPayloadAsync()
    {
        _storeLock.EnterReadLock();

        try
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
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public ValueTask<DataSetSnapshot> GetSnapshotAsync()
    {
        _storeLock.EnterReadLock();

        try
        {
            var items = new List<DataSetSnapshotItem>();

            var groupedSecrets = _secrets.GroupBy(x => x.EnvId);
            foreach (var group in groupedSecrets)
            {
                var envId = group.Key;
                var secrets = group.ToArray();

                var flags = _items.Values
                    .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag)
                    .Select(x => JsonSerializer.Deserialize<JsonObject>(x.JsonBytes)!)
                    .ToArray();

                var segments = _items.Values
                    .Where(x => x.EnvId == envId && x.Type == StoreItemType.Segment)
                    .Select(x => JsonSerializer.Deserialize<JsonObject>(x.JsonBytes)!)
                    .ToArray();

                var dataSetItem = new DataSetSnapshotItem(envId, secrets, flags, segments);
                items.Add(dataSetItem);
            }

            var dataSet = new DataSetSnapshot(DataSyncEventTypes.RpFull, items.ToArray());

            return ValueTask.FromResult(dataSet);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    public Task<IEnumerable<byte[]>> GetFlagsAsync(Guid envId, long timestamp)
    {
        _storeLock.EnterReadLock();

        try
        {
            var flags = _items.Values
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag && x.Timestamp > timestamp)
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(flags);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public Task<IEnumerable<byte[]>> GetFlagsAsync(IEnumerable<string> ids)
    {
        _storeLock.EnterReadLock();

        try
        {
            var flags = _items.Values
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(flags);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public Task<byte[]> GetSegmentAsync(string id)
    {
        _storeLock.EnterReadLock();

        try
        {
            // shared segments can cross multiple envs and we return the latest one
            var segment = _items.Values
                .Where(x => x.Id == id)
                .OrderByDescending(x => x.Timestamp)
                .First();

            return Task.FromResult(segment.JsonBytes);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public Task<IEnumerable<byte[]>> GetSegmentsAsync(Guid envId, long timestamp)
    {
        _storeLock.EnterReadLock();

        try
        {
            var segments = _items.Values
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Segment && x.Timestamp > timestamp)
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(segments);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public Task<Secret?> GetSecretAsync(string secretString)
    {
        _storeLock.EnterReadLock();

        try
        {
            var secretWithValue = _secrets.FirstOrDefault(x => x.Value == secretString);

            var secret = secretWithValue?.AsSecret();
            return Task.FromResult(secret);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    public ValueTask<string[]> GetFlagReferencesAsync(Guid envId, string segmentId)
    {
        _storeLock.EnterReadLock();

        try
        {
            var flagIds = _items.Values
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag)
                .Where(x => x.HasSegmentReference(segmentId))
                .Select(x => x.Id)
                .ToArray();

            return ValueTask.FromResult(flagIds);
        }
        finally
        {
            _storeLock.ExitReadLock();
        }
    }

    // Caller must hold _storeLock's write lock.
    private void UpsertItem(StoreItem item)
    {
        System.Diagnostics.Debug.Assert(_storeLock.IsWriteLockHeld);

        var key = new StoreItemKey(item.EnvId, item.Type, item.Key);
        if (!_items.TryGetValue(key, out var existing) || existing.Timestamp <= item.Timestamp)
        {
            // Replace the entire record so ID-based lookups use the new database ID too.
            _items[key] = item;
        }
    }
}
