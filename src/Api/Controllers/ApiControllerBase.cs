using Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/public/[controller]")]
public class ApiControllerBase : ControllerBase
{
    protected Guid EnvId
    {
        get
        {
            var authorization = Request.Headers.Authorization.ToString();
            return Secret.TryParse(authorization, out var envId) ? envId : Guid.Empty;
        }
    }

    protected string ApiKey => Request.Headers.Authorization.ToString();
}