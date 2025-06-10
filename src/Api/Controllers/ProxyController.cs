using Api.Store;
using Api.Models;
using System.Text.Json;
using Api.DataSynchronizer;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class ProxyController(
    IAgentStore agentStore,
    IDataSynchronizer dataSynchronizer,
    IConfiguration configuration,
    ILogger<ProxyController> logger)
    : ApiControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        if (ApiKey != configuration["ApiKey"])
        {
            return Unauthorized();
        }

        try
        {
            var state = new
            {
                data_version = agentStore.Version,
                data_last_synced_at = dataSynchronizer.LastSyncAt
            };

            return Ok(Status.Healthy(state));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred while checking the agent status.");
            return Ok(Status.Unhealthy());
        }
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(JsonElement jsonElement)
    {
        if (ApiKey != configuration["ApiKey"])
        {
            return Unauthorized();
        }

        var dataSet = DataSet.FromJson(jsonElement);
        await agentStore.PopulateAsync(dataSet);

        return Ok();
    }

    [HttpGet("backup")]
    public async Task<IActionResult> Backup()
    {
        if (ApiKey != configuration["ApiKey"])
        {
            return Unauthorized();
        }

        var snapshot = await agentStore.GetSnapshotAsync();
        return Ok(snapshot);
    }
}