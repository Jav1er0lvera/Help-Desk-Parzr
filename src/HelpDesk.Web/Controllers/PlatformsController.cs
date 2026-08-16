using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace HelpDesk.Web.Controllers;

[Authorize]
public class PlatformsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PlatformsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/v1/platforms");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        return StatusCode(500);
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(Guid id)
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync($"/api/v1/platforms/{id}/categories");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] PlatformWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { name = request.Name, description = request.Description });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/platforms", content);

        if (response.IsSuccessStatusCode) return Ok();
        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlatformWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { name = request.Name, description = request.Description });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/api/v1/platforms/{id}", content);

        if (response.IsSuccessStatusCode) return Ok();
        return StatusCode(500);
    }
}

public class PlatformWebRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}