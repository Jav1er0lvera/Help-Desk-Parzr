using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CategoriesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/v1/categories");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        return StatusCode(500);
    }
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CategoryWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { name = request.Name, description = request.Description, platformId = request.PlatformId });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/categories", content);

        if (response.IsSuccessStatusCode) return Ok();
        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { name = request.Name, description = request.Description, platformId = request.PlatformId });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/api/v1/categories/{id}", content);

        if (response.IsSuccessStatusCode) return Ok();
        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.DeleteAsync($"/api/v1/categories/{id}");

        if (response.IsSuccessStatusCode) return Ok();
        return StatusCode(500);
    }
}

public class CategoryWebRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
}