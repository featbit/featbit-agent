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
    public async Task<IActionResult> TrackAsync([FromBody] Insight[] insights)
    {
        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        var records = new List<Record>();
        foreach (var insight in insights)
        {
            if (!insight.IsValid())
            {
                continue;
            }

            var userRecord = new Record(RecordType.EndUser, insight.EndUserMessage(EnvId));
            var insightRecords =
                insight.InsightMessages(EnvId).Select(message => new Record(RecordType.Insights, message));

            records.Add(userRecord);
            records.AddRange(insightRecords);
        }

        await _repository.AddManyAsync(records);

        return Ok();
    }
}