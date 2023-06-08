using Domain.Shared;

namespace Api.Store;

public sealed class InMemoryStore : IStore
{
    private static readonly object WriteLock = new();

    private static volatile List<StoreItem> _flags = new();
    private static volatile List<StoreItem> _segments = new();

    public static void Populate(List<StoreItem> items)
    {
        lock (WriteLock)
        {
            _flags.Clear();
            _flags.AddRange(items.Where(x => x.Type == StoreItemType.Flag));

            _segments.Clear();
            _segments.AddRange(items.Where(x => x.Type == StoreItemType.Segment));
        }
    }

    public Task<IEnumerable<byte[]>> GetFlagsAsync(Guid envId, long timestamp)
    {
        var flags = _flags
            .Where(x => x.EnvId == envId && x.Timestamp > timestamp)
            .Select(x => x.JsonBytes);

        return Task.FromResult(flags);
    }

    public Task<IEnumerable<byte[]>> GetFlagsAsync(IEnumerable<string> ids)
    {
        var flags = _flags
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.JsonBytes);

        return Task.FromResult(flags);
    }

    public Task<byte[]> GetSegmentAsync(string id)
    {
        var segment = _segments.First(x => x.Id == id).JsonBytes;
        return Task.FromResult(segment);
    }

    public Task<IEnumerable<byte[]>> GetSegmentsAsync(Guid envId, long timestamp)
    {
        var segments = _segments
            .Where(x => x.EnvId == envId && x.Timestamp > timestamp)
            .Select(x => x.JsonBytes);

        return Task.FromResult(segments);
    }
}