using HelpDesk.Domain.Comments;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/tickets/{ticketId}/comments")]
public class CommentsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;

    public CommentsController(HelpDeskDbContext db)
    {
        _db = db;
    }

    // GET api/v1/tickets/{ticketId}/comments
    [HttpGet]
    public async Task<IActionResult> GetByTicket(Guid ticketId)
    {
        var comments = await _db.Comments
            .Where(c => c.TicketId == ticketId)
            .Join(_db.Users,
                c => c.AuthorId,
                u => u.Id,
                (c, u) => new
                {
                    c.Id,
                    c.Message,
                    c.TicketId,
                    c.AuthorId,
                    c.CreatedAt,
                    AuthorName = u.FirstName + " " + u.LastName
                })
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments);
    }

    // POST api/v1/tickets/{ticketId}/comments
    [HttpPost]
    public async Task<IActionResult> Create(Guid ticketId, [FromBody] CreateCommentRequest request)
    {
        var ticketExists = await _db.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists) return NotFound();

        var comment = new Comment
        {
            Message = request.Message,
            TicketId = ticketId,
            AuthorId = request.AuthorId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Ok(comment);
    }
}

public class CreateCommentRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
}