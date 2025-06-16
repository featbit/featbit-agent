using Domain.EndUsers;
using Microsoft.AspNetCore.Mvc;
using Streaming.Services;

namespace Api.Controllers;

public class SdkController(IDataSyncService dataSyncService) : ApiControllerBase
{
    [HttpGet("server/latest-all")]
    public async Task<IActionResult> GetServerSideSdkPayloadAsync([FromQuery] long timestamp = 0)
    {
        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        var payload = await dataSyncService.GetServerSdkPayloadAsync(EnvId, timestamp);
        if (payload.IsEmpty())
        {
            return Ok();
        }

        var bootstrap = new
        {
            messageType = "data-sync",
            data = payload
        };

        return new JsonResult(bootstrap);
    }

    [HttpPost("client/latest-all")]
    public async Task<IActionResult> GetClientSdkPayloadAsync(EndUser endUser, [FromQuery] long timestamp = 0)
    {
        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (!endUser.IsValid())
        {
            return BadRequest("invalid end user");
        }

        var payload = await dataSyncService.GetClientSdkPayloadAsync(EnvId, endUser, timestamp);
        if (payload.IsEmpty())
        {
            return Ok();
        }

        var bootstrap = new
        {
            messageType = "data-sync",
            data = payload
        };

        return new JsonResult(bootstrap);
    }
}