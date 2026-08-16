using Refit;

namespace HelpDesk.SDK.Comments;

public interface ICommentsApi
{
    [Get("/api/v1/tickets/{ticketId}/comments")]
    Task<List<CommentDto>> GetByTicketAsync(Guid ticketId);

    [Post("/api/v1/tickets/{ticketId}/comments")]
    Task<CommentDto> CreateAsync(Guid ticketId, [Body] CreateCommentDto request);
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentDto
{
    public string Message { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
}