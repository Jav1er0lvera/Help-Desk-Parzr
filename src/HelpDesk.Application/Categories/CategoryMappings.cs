using HelpDesk.Domain.Categories;

namespace HelpDesk.Application.Categories;

public static class CategoryMappings
{
    // Entidad (Domain) → DTO de respuesta (Application). El único mapeo de salida del servidor.
    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.Description, c.PlatformId, c.CreatedAt);
}
