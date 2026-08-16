using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HelpDesk.Web.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TicketsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateTicketWebRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var payload = new
        {
            title = request.Title,
            description = request.Description,
            priority = request.Priority,
            categoryId = request.CategoryId,
            createdByUserId = userId
        };

        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/tickets", content);

        if (response.IsSuccessStatusCode)
            return Ok();

        return StatusCode(500);
    }

    [HttpGet]
    public async Task<IActionResult> Comments(Guid id)
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync($"/api/v1/tickets/{id}/comments");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentWebRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new
        {
            message = request.Message,
            authorId = userId
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/v1/tickets/{id}/comments", content);

        if (response.IsSuccessStatusCode)
            return Ok();

        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { status = request.Status });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PatchAsync($"/api/v1/tickets/{id}/status", content);

        if (response.IsSuccessStatusCode)
            return Ok();

        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.DeleteAsync($"/api/v1/tickets/{id}");

        if (response.IsSuccessStatusCode)
            return Ok();

        return StatusCode(500);
    }

    [HttpGet]
    public async Task<IActionResult> GetSoporte()
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/v1/users/soporte");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        return StatusCode(500);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketWebRequest request)
    {
        var client = _httpClientFactory.CreateClient("api");
        var json = JsonSerializer.Serialize(new { assignedToUserId = request.AssignedToUserId });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PatchAsync($"/api/v1/tickets/{id}/assign", content);

        if (response.IsSuccessStatusCode)
            return Ok();

        return StatusCode(500);
    }
}

public class CreateTicketWebRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public Guid? CategoryId { get; set; }
}

public class AddCommentWebRequest
{
    public string Message { get; set; } = string.Empty;
}

public class UpdateStatusWebRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignTicketWebRequest
{
    public Guid AssignedToUserId { get; set; }
}