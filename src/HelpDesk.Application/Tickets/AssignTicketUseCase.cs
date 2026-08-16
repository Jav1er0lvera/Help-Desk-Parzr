using HelpDesk.Domain.Interfaces;
using HelpDesk.Domain.Tickets;
using MediatR;

namespace HelpDesk.Application.Tickets;

public class AssignTicketCommand : IRequest<Ticket>
{
    public Guid TicketId { get; set; }
    public Guid AssignedToUserId { get; set; }
}

public class AssignTicketHandler : IRequestHandler<AssignTicketCommand, Ticket>
{
    private readonly ITicketRepository _ticketRepository;

    public AssignTicketHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Ticket> Handle(AssignTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);
        if (ticket is null) throw new Exception("Ticket no encontrado.");

        ticket.AssignedToUserId = command.AssignedToUserId;
        ticket.Status = "En progreso";

        return await _ticketRepository.UpdateAsync(ticket);
    }
}