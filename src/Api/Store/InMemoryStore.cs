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

    public bool Initialized { get; private set; }

    public long Version { get; private set; }

    public string Serves
    {
        get
        {
            lock (_writeLock)
            {
                var envs = _secrets
                    .GroupBy(x => x.EnvId)
                    .Select(envSecrets => envSecrets.First())
                    .Select(envSecret => $"{envSecret.ProjectKey}:{envSecret.EnvKey}");

                var served = string.Join(",", envs);
                return served;
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
        lock (_writeLock)
        {
            _secrets.Clear();
            _items.Clear();

            var flagIndexes = new Dictionary<(Guid EnvId, string? Key), int>();

            foreach (var item in dataSet.Items)
            {
                _secrets.AddRange(item.Secrets);

                foreach (var flag in item.FeatureFlags)
                {
                    // Index flags during full loading to avoid scanning the growing list.
                    var identity = (flag.EnvId, flag.Key);
                    if (!flagIndexes.TryGetValue(identity, out var index))
                    {
                        flagIndexes.Add(identity, _items.Count);
                        _items.Add(flag);
                    }
                    else if (_items[index].Timestamp <= flag.Timestamp)
                    {
                        _items[index] = flag;
                    }
                }
                _items.AddRange(item.Segments);

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

        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(StoreItem[] items)
    {
        lock (_writeLock)
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

        return ValueTask.CompletedTask;
    }

    private void UpsertItem(StoreItem item)
    {
        // Flags are identified by key in SDKs; recreating one changes its database ID.
        // Shared segments still use their database ID within each environment.
        var index = _items.FindIndex(x =>
            x.EnvId == item.EnvId && x.Type == item.Type &&
            (item.Type == StoreItemType.Flag ? x.Key == item.Key : x.Id == item.Id));

        if (index < 0)
        {
            _items.Add(item);
        }
        else if (_items[index].Timestamp <= item.Timestamp)
        {
            // Replace the entire record so ID-based lookups use the new database ID too.
            _items[index] = item;
        }
    }

    public ValueTask<byte[]> GetDataSyncPayloadAsync()
    {
        lock (_writeLock)
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
        lock (_writeLock)
        {
            var flags = _items
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag && x.Timestamp > timestamp)
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(flags);
        }
    }

    public Task<IEnumerable<byte[]>> GetFlagsAsync(IEnumerable<string> ids)
    {
        lock (_writeLock)
        {
            var flags = _items
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(flags);
        }
    }

    public Task<byte[]> GetSegmentAsync(string id)
    {
        lock (_writeLock)
        {
            // shared segments can cross multiple envs and we return the latest one
            var segment = _items
                .Where(x => x.Id == id)
                .OrderByDescending(x => x.Timestamp)
                .First();

            return Task.FromResult(segment.JsonBytes);
        }
    }

    public Task<IEnumerable<byte[]>> GetSegmentsAsync(Guid envId, long timestamp)
    {
        lock (_writeLock)
        {
            var segments = _items
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Segment && x.Timestamp > timestamp)
                .Select(x => x.JsonBytes)
                .ToArray();

            return Task.FromResult<IEnumerable<byte[]>>(segments);
        }
    }

    public Task<Secret?> GetSecretAsync(string secretString)
    {
        lock (_writeLock)
        {
            var secretWithValue = _secrets.FirstOrDefault(x => x.Value == secretString);

            var secret = secretWithValue?.AsSecret();
            return Task.FromResult(secret);
        }
    }

    public ValueTask<string[]> GetFlagReferencesAsync(Guid envId, string segmentId)
    {
        lock (_writeLock)
        {
            var flagIds = _items
                .Where(x => x.EnvId == envId && x.Type == StoreItemType.Flag)
                .Where(x => x.HasSegmentReference(segmentId))
                .Select(x => x.Id)
                .ToArray();

            return ValueTask.FromResult(flagIds);
        }
    }
}