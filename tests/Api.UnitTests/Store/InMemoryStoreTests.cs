using System.Text.Json;
using Api.Store;
using Xunit;

namespace Api.UnitTests.Store;

public class InMemoryStoreTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecreatedFlags_KeepNewestRecord_InFullAndIncrementalSync(bool full)
    {
        var env = Guid.NewGuid();
        var old = Item("old", env, "flag", 10, "same", archived: true);
        var current = Item("new", env, "flag", 20, "same");
        var stale = Item("stale", env, "flag", 15, "same", archived: true);
        var store = new InMemoryStore();

        await Load(store, full, old, current, stale);

        var bytes = Assert.Single(await store.GetFlagsAsync(env, 0));
        Assert.Equal(current.JsonBytes, bytes);
        Assert.Empty(await store.GetFlagsAsync(new[] { "old" }));
        Assert.Single(await store.GetFlagsAsync(new[] { "new" }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Segments_UpdateById_KeepingNewestRecord(bool full)
    {
        var env = Guid.NewGuid();
        var first = Item("a", env, "segment", 10);
        var second = Item("b", env, "segment", 11);
        var updated = Item("a", env, "segment", 20);
        var store = new InMemoryStore();

        await Load(store, full, first, second, updated, first);

        Assert.Equal(2, (await store.GetSegmentsAsync(env, 0)).Count());
        Assert.Equal(updated.JsonBytes, await store.GetSegmentAsync("a"));
        Assert.Equal(second.JsonBytes, await store.GetSegmentAsync("b"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Items_AreIsolatedByEnvironmentAndType(bool full)
    {
        var env = Guid.NewGuid();
        var otherEnv = Guid.NewGuid();
        var store = new InMemoryStore();
        await Load(store, full,
            Item("flag", env, "flag", 10, "same"),
            Item("same", env, "segment", 10),
            Item("same", otherEnv, "segment", 10),
            Item("other-id", env, "segment", 10));

        Assert.Single(await store.GetFlagsAsync(env, 0));
        Assert.Equal(2, (await store.GetSegmentsAsync(env, 0)).Count());
        Assert.Single(await store.GetSegmentsAsync(otherEnv, 0));
    }

    [Theory]
    [InlineData("flag", false)]
    [InlineData("flag", true)]
    [InlineData("segment", false)]
    [InlineData("segment", true)]
    public async Task EqualTimestamps_KeepLastReceivedRecord(string type, bool full)
    {
        var env = Guid.NewGuid();
        var last = Item(type == "segment" ? "first" : "last", env, type, 10, "same", archived: true);
        var store = new InMemoryStore();
        await Load(store, full, Item("first", env, type, 10, "same"), last);
        Assert.Equal(last.JsonBytes, Assert.Single(await Read(store, env, type)));
    }

    [Theory]
    [InlineData("flag")]
    [InlineData("segment")]
    public async Task QueryResult_RemainsStable_AfterUpdatesAndReload(string type)
    {
        var env = Guid.NewGuid();
        var original = Item("original", env, type, 10, "same");
        var store = new InMemoryStore();
        await store.UpdateAsync([original]);
        var result = await Read(store, env, type);
        await store.UpdateAsync([Item("replacement", env, type, 20, "same")]);
        await Load(store, true, Item("reload", env, type, 30, "other"));
        Assert.Equal(original.JsonBytes, Assert.Single(result));
    }

    private static async Task Load(InMemoryStore store, bool full, params StoreItem[] items)
    {
        if (!full)
        {
            // Separate deliveries also exercise replacement across update calls.
            foreach (var item in items) await store.UpdateAsync([item]);
            return;
        }
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            eventType = "rp-full",
            items = items.GroupBy(x => x.EnvId).Select(group => new
            {
                envId = group.Key,
                featureFlags = group.Where(x => x.Type == "flag").Select(x => JsonSerializer.Deserialize<JsonElement>(x.JsonBytes)),
                segments = group.Where(x => x.Type == "segment").Select(x => JsonSerializer.Deserialize<JsonElement>(x.JsonBytes))
            })
        }));
        await store.PopulateAsync(DataSet.FromJson(json.RootElement));
    }

    private static Task<IEnumerable<byte[]>> Read(InMemoryStore store, Guid env, string type) =>
        type == "flag" ? store.GetFlagsAsync(env, 0) : store.GetSegmentsAsync(env, 0);

    private static StoreItem Item(string id, Guid env, string type, long timestamp,
        string? key = null, bool archived = false)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = id, ["envId"] = env,
            ["updatedAt"] = DateTimeOffset.FromUnixTimeMilliseconds(timestamp),
            ["isArchived"] = archived
        };
        if (type == "flag") payload["key"] = key;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return StoreItem.Of(json.RootElement, type);
    }
}
