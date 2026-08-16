using HelpDesk.Domain.Interfaces;
using HelpDesk.Domain.Users;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HelpDeskDbContext _db;

    public UserRepository(HelpDeskDbContext db)
    {
        _db = db;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _db.Users.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<User>> GetSoporteAsync()
    {
        return await _db.Users
            .Where(u => u.Role == HelpDesk.Domain.Enums.UserRole.Soporte)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }
}