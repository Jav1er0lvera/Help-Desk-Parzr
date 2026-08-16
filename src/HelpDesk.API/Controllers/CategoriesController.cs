using HelpDesk.Domain.Categories;
using HelpDesk.Domain.Platforms;
using HelpDesk.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly HelpDeskDbContext _db;

    public CategoriesController(HelpDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .Where(c => !c.IsDeleted)
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (category is null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        var exists = await _db.Categories.AnyAsync(c => c.Name == request.Name);
        if (exists)
            return BadRequest("Ya existe una categoría con ese nombre.");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            PlatformId = request.PlatformId
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(category);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.PlatformId = request.PlatformId;
        await _db.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
}