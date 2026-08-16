using Refit;

namespace HelpDesk.SDK.Platforms;

public interface IPlatformsApi
{
    [Get("/api/v1/platforms")]
    Task<List<PlatformDto>> GetAllAsync();

    [Post("/api/v1/platforms")]
    Task<PlatformDto> CreateAsync([Body] PlatformRequestDto request);

    [Patch("/api/v1/platforms/{id}")]
    Task<PlatformDto> UpdateAsync(Guid id, [Body] PlatformRequestDto request);

    [Get("/api/v1/platforms/{id}/categories")]
    Task<List<CategoryDto>> GetCategoriesAsync(Guid id);
}

public class PlatformDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlatformRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
    public DateTime CreatedAt { get; set; }
}