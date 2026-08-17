using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Web.Controllers;

[Authorize(Roles = "Supervisor")]
public class MetricsController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
