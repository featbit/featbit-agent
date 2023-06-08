using Api.Store;
using Api.Models;
using Api.Persistence;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

public class ProxyController : ApiControllerBase
{
    private readonly IRepository _repository;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IRepository repository, ILogger<ProxyController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<Status> GetStatusAsync()
    {
        try
        {
            var lastSync = await _repository.QueryableOf<SyncHistory>()
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            return Status.Healthy(lastSync?.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while checking the agent status.");
            return Status.UnHealthy(null);
        }
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(JsonElement jsonElement)
    {
        var storeItems = new List<StoreItem>();
        var storeItemBackups = new List<StoreItemBackup>();

        foreach (var element in jsonElement.EnumerateArray())
        {
            var envId = element.GetProperty("envId").GetGuid();
            var flags = element.GetProperty("flags").EnumerateArray();
            var segments = element.GetProperty("segments").EnumerateArray();

            CreateStoreItems(envId, flags, StoreItemType.Flag);
            CreateStoreItems(envId, segments, StoreItemType.Segment);
        }

        void CreateStoreItems(Guid envId, JsonElement.ArrayEnumerator elements, string itemType)
        {
            foreach (var element in elements)
            {
                var id = element.GetProperty("id").GetString()!;
                var timestamp = element.GetProperty("updatedAt").GetDateTimeOffset().ToUnixTimeMilliseconds();
                var bytes = JsonSerializer.SerializeToUtf8Bytes(element);

                storeItems.Add(new StoreItem(id, envId, itemType, timestamp, bytes));
                storeItemBackups.Add(new StoreItemBackup(itemType, element.GetRawText()));
            }
        }

        // populate InMemoryStore
        InMemoryStore.Populate(storeItems);

        // update store item backups
        await _repository.TruncateAsync<StoreItemBackup>();
        await _repository.AddManyAsync(storeItemBackups);

        // add sync history
        var syncHistory = new SyncHistory { CreatedAt = DateTime.UtcNow };
        await _repository.AddAsync(syncHistory);

        var vms = storeItems.Select(x => new
        {
            x.Id,
            x.Type,
            x.Timestamp
        });
        return Ok(vms);
    }
}