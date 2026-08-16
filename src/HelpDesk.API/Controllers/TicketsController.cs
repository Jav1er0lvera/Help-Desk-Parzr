using HelpDesk.Application.Tickets;
using HelpDesk.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly IMediator _mediator;

    public TicketsController(HelpDeskDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await (
            from t in _db.Tickets
            join creator in _db.Users on t.CreatedByUserId equals creator.Id into creators
            from creator in creators.DefaultIfEmpty()
            join assignee in _db.Users on t.AssignedToUserId equals assignee.Id into assignees
            from assignee in assignees.DefaultIfEmpty()
            join category in _db.Categories on t.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            join platform in _db.Platforms on category.PlatformId equals platform.Id into platforms
            from platform in platforms.DefaultIfEmpty()
            select new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Priority,
                t.Status,
                t.CreatedByUserId,
                t.AssignedToUserId,
                t.CategoryId,
                t.CreatedAt,
                t.UpdatedAt,
                t.ResolvedAt,
                CreatedByName = creator != null ? creator.FirstName + " " + creator.LastName : "Desconocido",
                AssignedToName = assignee != null ? assignee.FirstName + " " + assignee.LastName : "Sin asignar",
                CategoryName = category != null ? category.Name : "Sin categoría",
                PlatformName = platform != null ? platform.Name : "Sin plataforma"
            }
        ).ToListAsync();

        return Ok(tickets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await (
            from t in _db.Tickets
            where t.Id == id
            join creator in _db.Users on t.CreatedByUserId equals creator.Id into creators
            from creator in creators.DefaultIfEmpty()
            join assignee in _db.Users on t.AssignedToUserId equals assignee.Id into assignees
            from assignee in assignees.DefaultIfEmpty()
            select new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Priority,
                t.Status,
                t.CreatedByUserId,
                t.AssignedToUserId,
                t.CreatedAt,
                t.UpdatedAt,
                t.ResolvedAt,
                CreatedByName = creator != null ? creator.FirstName + " " + creator.LastName : "Desconocido",
                AssignedToName = assignee != null ? assignee.FirstName + " " + assignee.LastName : "Sin asignar"
            }
        ).FirstOrDefaultAsync();

        if (ticket is null) return NotFound();
        return Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request)
    {
        var ticket = await _mediator.Send(new CreateTicketCommand
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            CreatedByUserId = request.CreatedByUserId,
            CategoryId = request.CategoryId
        });

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        ticket.Status = request.Status;

        if (request.Status == "Resuelto")
            ticket.ResolvedAt = DateTime.UtcNow;
        else
            ticket.ResolvedAt = null;

        await _db.SaveChangesAsync();
        return Ok(ticket);
    }

    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketRequest request)
    {
        var ticket = await _mediator.Send(new AssignTicketCommand
        {
            TicketId = id,
            AssignedToUserId = request.AssignedToUserId
        });

        return Ok(ticket);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public Guid CreatedByUserId { get; set; }
    public Guid? CategoryId { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignTicketRequest
{
    public Guid AssignedToUserId { get; set; }
}