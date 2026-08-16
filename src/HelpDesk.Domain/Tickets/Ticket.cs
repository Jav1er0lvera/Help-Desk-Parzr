using HelpDesk.Domain.Common;

namespace HelpDesk.Domain.Tickets;

public class Ticket : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public string Status { get; set; } = "Abierto";
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? ResolvedAt { get; set; }
}