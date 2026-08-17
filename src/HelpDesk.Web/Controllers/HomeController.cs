using System.Diagnostics;
using System.Text.Json;

using HelpDesk.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Dashboard() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TicketDetail(Guid id)
    {
        var client = _httpClientFactory.CreateClient("api");
        TicketViewModel? ticket = null;

        try
        {
            var response = await client.GetAsync($"/api/v1/tickets/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                ticket = JsonSerializer.Deserialize<TicketViewModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }

        if (ticket is null) return NotFound();
        return View(ticket);
    }
}

public class TicketViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
}