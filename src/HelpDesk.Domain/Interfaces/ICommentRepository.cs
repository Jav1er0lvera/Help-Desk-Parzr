using HelpDesk.Domain.Comments;

namespace HelpDesk.Domain.Interfaces;

public interface ICommentRepository
{
    Task<List<Comment>> GetByTicketIdAsync(Guid ticketId);
    Task<Comment> CreateAsync(Comment comment);
}