using HelpDesk.Domain.Interfaces;
using HelpDesk.Domain.Tickets;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly HelpDeskDbContext _db;

    public TicketRepository(HelpDeskDbContext db)
    {
        _db = db;
    }

    public async Task<List<Ticket>> GetAllAsync()
    {
        return await _db.Tickets.ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        return await _db.Tickets.FindAsync(id);
    }

    public async Task<Ticket> CreateAsync(Ticket ticket)
    {
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task DeleteAsync(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket is null) return;
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();
    }
}