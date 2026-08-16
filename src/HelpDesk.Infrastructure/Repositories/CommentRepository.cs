using HelpDesk.Domain.Comments;
using HelpDesk.Domain.Interfaces;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly HelpDeskDbContext _db;

    public CommentRepository(HelpDeskDbContext db)
    {
        _db = db;
    }

    public async Task<List<Comment>> GetByTicketIdAsync(Guid ticketId)
    {
        return await _db.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }
}