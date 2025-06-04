using Api.Store;
using Api.Models;
using System.Text.Json;
using Api.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class ProxyController(
    IAgentStore agentStore,
    IConfiguration configuration,
    IDataChangeNotifier dataChangeNotifier,
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
            return Ok(Status.Healthy(agentStore.UpdatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred while checking the agent status.");
            return Ok(Status.UnHealthy(null));
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

        // notify all environments that have been updated
        var envIds = dataSet.Items.Select(x => x.EnvId).Distinct();
        await dataChangeNotifier.NotifyAsync(envIds, 0);

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