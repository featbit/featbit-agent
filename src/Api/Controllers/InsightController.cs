using System.Text.Json;
using Api.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Api.Controllers;

public class InsightController : ApiControllerBase
{
    private readonly Uri _eventUri;
    private readonly bool _forwardEvents;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InsightController> _logger;

    public InsightController(
        IOptions<AgentOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<InsightController> logger)
    {
        var optionsValue = options.Value;

        var eventUri = optionsValue.EventUri?.TrimEnd('/');

        _forwardEvents = optionsValue.ForwardEvents;
        _eventUri = new Uri($"{eventUri}/api/public/insight/track");

        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("track")]
    public async Task<IActionResult> TrackAsync([FromBody] JsonElement jsonElement)
    {
        if (!_forwardEvents)
        {
            // If event forwarding is disabled, we simply return OK without processing the event.
            return Ok();
        }

        if (EnvId == Guid.Empty)
        {
            return Unauthorized();
        }

        var httpClient = _httpClientFactory.CreateClient();

        var authorization = Request.Headers[HeaderNames.Authorization].ToString();

        httpClient.Timeout = TimeSpan.FromSeconds(5);
        httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "featbit-agent");
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, authorization);

        try
        {
            var response = await httpClient.PostAsJsonAsync(_eventUri, jsonElement);

            return response.IsSuccessStatusCode
                ? Ok()
                : StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while forwarding event to {EventUri}", _eventUri);
            return StatusCode(500);
        }
    }
}