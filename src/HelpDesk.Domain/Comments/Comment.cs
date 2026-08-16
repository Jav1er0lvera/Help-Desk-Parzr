using HelpDesk.Domain.Common;

namespace HelpDesk.Domain.Comments;

public class Comment : BaseEntity
{
    public string Message { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
}