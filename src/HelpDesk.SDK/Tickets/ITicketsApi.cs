using Refit;

namespace HelpDesk.SDK.Tickets;

public interface ITicketsApi
{
    [Get("/api/v1/tickets")]
    Task<List<TicketDto>> GetAllAsync();

    [Get("/api/v1/tickets/{id}")]
    Task<TicketDto> GetByIdAsync(Guid id);

    [Post("/api/v1/tickets")]
    Task<TicketDto> CreateAsync([Body] CreateTicketDto request);

    [Patch("/api/v1/tickets/{id}/status")]
    Task<TicketDto> UpdateStatusAsync(Guid id, [Body] UpdateStatusDto request);

    [Patch("/api/v1/tickets/{id}/assign")]
    Task<TicketDto> AssignAsync(Guid id, [Body] AssignTicketDto request);

    [Delete("/api/v1/tickets/{id}")]
    Task DeleteAsync(Guid id);
}

public class TicketDto
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

public class CreateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public Guid CreatedByUserId { get; set; }
    public Guid? CategoryId { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class AssignTicketDto
{
    public Guid AssignedToUserId { get; set; }
}