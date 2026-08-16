using HelpDesk.Application.Greetings;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class GreetingsController(IGreetingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GreetingDto>> GetCurrent(CancellationToken cancellationToken)
        => Ok(await service.GetCurrentAsync(cancellationToken));
}
