using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class InsightController : ApiControllerBase
{
    [HttpPost("track")]
    public IActionResult TrackAsync([FromBody] JsonElement jsonElement)
    {
        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        // TODO: transfer insight data to ELS

        return Ok();
    }
}