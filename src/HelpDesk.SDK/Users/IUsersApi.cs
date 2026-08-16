using Refit;

namespace HelpDesk.SDK.Users;

public interface IUsersApi
{
    [Get("/api/v1/users")]
    Task<List<UserDto>> GetAllAsync();

    [Get("/api/v1/users/{id}")]
    Task<UserDto> GetByIdAsync(Guid id);

    [Get("/api/v1/users/soporte")]
    Task<List<UserDto>> GetSoporteAsync();
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}