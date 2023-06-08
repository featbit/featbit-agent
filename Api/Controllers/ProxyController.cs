using System.Text.Json;
using Api.Persistence;
using Api.Store;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class ProxyController : ApiControllerBase
{
    private readonly IRepository _repository;

    public ProxyController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("status")]
    public IActionResult GetStatusAsync()
    {
        var status = new
        {
            lastSyncTime = DateTime.UtcNow
        };

        return new JsonResult(status);
    }

    [HttpPost("bootstrap")]
    public IActionResult Bootstrap(JsonElement jsonElement)
    {
        var items = new List<StoreItem>();

        foreach (var element in jsonElement.EnumerateArray())
        {
            var envId = element.GetProperty("envId").GetGuid();
            var flags = element.GetProperty("flags").EnumerateArray();
            var segments = element.GetProperty("segments").EnumerateArray();

            items.AddRange(CreateStoreItems(envId, flags, StoreItemType.Flag));
            items.AddRange(CreateStoreItems(envId, segments, StoreItemType.Segment));
        }

        List<StoreItem> CreateStoreItems(Guid envId, JsonElement.ArrayEnumerator elements, string itemType)
        {
            var storeItems = new List<StoreItem>();

            foreach (var element in elements)
            {
                var id = element.GetProperty("id").GetString()!;
                var timestamp = element.GetProperty("updatedAt").GetDateTimeOffset().ToUnixTimeMilliseconds();
                var bytes = JsonSerializer.SerializeToUtf8Bytes(element);

                storeItems.Add(new StoreItem(id, envId, itemType, timestamp, bytes));
            }

            return storeItems;
        }

        // populate store
        InMemoryStore.Populate(items);

        return Ok(items);
    }
}