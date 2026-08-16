using HelpDesk.Domain.Users;

namespace HelpDesk.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetSoporteAsync();
    Task<User> CreateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}