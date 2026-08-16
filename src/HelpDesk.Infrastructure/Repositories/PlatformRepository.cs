using HelpDesk.Domain.Interfaces;
using HelpDesk.Domain.Platforms;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class PlatformRepository : IPlatformRepository
{
    private readonly HelpDeskDbContext _db;

    public PlatformRepository(HelpDeskDbContext db)
    {
        _db = db;
    }

    public async Task<List<Platform>> GetAllAsync()
    {
        return await _db.Platforms.ToListAsync();
    }

    public async Task<Platform?> GetByIdAsync(Guid id)
    {
        return await _db.Platforms.FindAsync(id);
    }

    public async Task<Platform> CreateAsync(Platform platform)
    {
        _db.Platforms.Add(platform);
        await _db.SaveChangesAsync();
        return platform;
    }

    public async Task<Platform> UpdateAsync(Platform platform)
    {
        _db.Platforms.Update(platform);
        await _db.SaveChangesAsync();
        return platform;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _db.Platforms.AnyAsync(p => p.Name == name);
    }
}