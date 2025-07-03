using Api.Store;
using System.Text.Json;
using Api.Messaging;
using Api.Services;
using Api.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

public class ProxyController(
    IAgentStore agentStore,
    IStatusProvider statusProvider,
    IDataChangeNotifier dataChangeNotifier,
    IOptions<AgentOptions> options)
    : ApiControllerBase
{
    private readonly AgentOptions _options = options.Value;

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        if (ApiKey != _options.ApiKey)
        {
            return Unauthorized();
        }

        var status = statusProvider.GetStatus();
        return Ok(status);
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(JsonElement jsonElement)
    {
        if (ApiKey != _options.ApiKey)
        {
            return Unauthorized();
        }

        var dataSet = DataSet.FromJson(jsonElement);
        await agentStore.PopulateAsync(dataSet);

        // Notify all environments about the data change
        var envIds = dataSet.Items.Select(x => x.EnvId);
        foreach (var envId in envIds)
        {
            await dataChangeNotifier.NotifyAsync(envId);
        }

        return Ok();
    }

    [HttpGet("backup")]
    public async Task<IActionResult> Backup()
    {
        if (ApiKey != _options.ApiKey)
        {
            return Unauthorized();
        }

        var snapshot = await agentStore.GetSnapshotAsync();
        return Ok(snapshot);
    }
}