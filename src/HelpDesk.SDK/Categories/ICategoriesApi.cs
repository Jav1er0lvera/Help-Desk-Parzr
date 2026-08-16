using Refit;

namespace HelpDesk.SDK.Categories;

public interface ICategoriesApi
{
    [Get("/api/v1/categories")]
    Task<List<CategoryDto>> GetAllAsync();

    [Post("/api/v1/categories")]
    Task<CategoryDto> CreateAsync([Body] CategoryRequestDto request);

    [Patch("/api/v1/categories/{id}")]
    Task<CategoryDto> UpdateAsync(Guid id, [Body] CategoryRequestDto request);

    [Delete("/api/v1/categories/{id}")]
    Task DeleteAsync(Guid id);
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
}