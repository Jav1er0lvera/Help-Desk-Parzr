using HelpDesk.Domain.Interfaces;
using HelpDesk.Domain.Tickets;
using MediatR;

namespace HelpDesk.Application.Tickets;

public class CreateTicketCommand : IRequest<Ticket>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public Guid CreatedByUserId { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CreateTicketHandler : IRequestHandler<CreateTicketCommand, Ticket>
{
    private readonly ITicketRepository _ticketRepository;

    public CreateTicketHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Ticket> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = new Ticket
        {
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Priority = command.Priority,
            Status = "Abierto",
            CreatedByUserId = command.CreatedByUserId,
            CategoryId = command.CategoryId
        };

        return await _ticketRepository.CreateAsync(ticket);
    }
}