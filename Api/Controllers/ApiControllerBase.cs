using Microsoft.AspNetCore.Mvc;
using Streaming.Shared;

namespace Api.Controllers;

[Route("api/public/[controller]")]
public class ApiControllerBase : ControllerBase
{
    protected Guid EnvId
    {
        get
        {
            var authorization = Request.Headers.Authorization.ToString();
            return Secret.TryParse(authorization, out var secret) ? secret.EnvId : Guid.Empty;
        }
    }

    protected string ApiKey => Request.Headers.Authorization.ToString();
}