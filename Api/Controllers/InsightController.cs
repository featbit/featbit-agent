using System.Text.Json;
using Api.Shared;
using Api.Persistence;
using Domain.Insights;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class InsightController : ApiControllerBase
{
    private readonly IRepository _repository;

    public InsightController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("track")]
    public async Task<IActionResult> TrackAsync([FromBody] JsonElement element)
    {
        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        var record = new Record(element.GetRawText());
        await _repository.AddAsync(record);

        return Ok();
    }
}