using HelpDesk.Domain.Platforms;
using HelpDesk.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/platforms")]
public class PlatformsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;

    public PlatformsController(HelpDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var platforms = await _db.Platforms.ToListAsync();
        return Ok(platforms);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlatformRequest request)
    {
        var exists = await _db.Platforms.AnyAsync(p => p.Name == request.Name);
        if (exists)
            return BadRequest("Ya existe una plataforma con ese nombre.");

        var platform = new Platform
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _db.Platforms.Add(platform);
        await _db.SaveChangesAsync();
        return Ok(platform);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlatformRequest request)
    {
        var platform = await _db.Platforms.FindAsync(id);
        if (platform is null) return NotFound();

        platform.Name = request.Name.Trim();
        platform.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();
        return Ok(platform);
    }

    // GET api/v1/platforms/{id}/categories
    [HttpGet("{id}/categories")]
    public async Task<IActionResult> GetCategories(Guid id)
    {
        var categories = await _db.Categories
            .Where(c => c.PlatformId == id && !c.IsDeleted)
            .ToListAsync();
        return Ok(categories);
    }
}

public class PlatformRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}