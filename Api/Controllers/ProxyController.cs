using Api.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class ProxyController : ApiControllerBase
{
    private readonly IRepository _repository;

    public ProxyController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("status")]
    public IActionResult GetStatusAsync()
    {
        var status = new
        {
            lastSyncTime = DateTime.UtcNow
        };

        return new JsonResult(status);
    }

    [HttpPost("bootstrap")]
    public IActionResult BootstrapAsync()
    {
        return Ok();
    }
}