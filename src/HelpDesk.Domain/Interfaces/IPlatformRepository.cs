using HelpDesk.Domain.Platforms;

namespace HelpDesk.Domain.Interfaces;

public interface IPlatformRepository
{
    Task<List<Platform>> GetAllAsync();
    Task<Platform?> GetByIdAsync(Guid id);
    Task<Platform> CreateAsync(Platform platform);
    Task<Platform> UpdateAsync(Platform platform);
    Task<bool> ExistsByNameAsync(string name);
}