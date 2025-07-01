using Api.Store;
using System.Text.Json;
using Api.Services;
using Api.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

public class ProxyController(
    IAgentStore agentStore,
    IStatusProvider statusProvider,
    IOptions<AgentOptions> options)
    : ApiControllerBase
{
    private readonly AgentOptions _agentOptions = options.Value;

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        if (ApiKey != _agentOptions.ApiKey)
        {
            return Unauthorized();
        }

        var status = statusProvider.GetStatus();
        return Ok(status);
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(JsonElement jsonElement)
    {
        if (ApiKey != _agentOptions.ApiKey)
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
        if (ApiKey != _agentOptions.ApiKey)
        {
            return Unauthorized();
        }

        var snapshot = await agentStore.GetSnapshotAsync();
        return Ok(snapshot);
    }
}