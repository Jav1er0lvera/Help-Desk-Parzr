using HelpDesk.Application.Categories;

namespace HelpDesk.API.Mapping;

/// <summary>Contrato de entrada HTTP de Categories + su traducción a los Commands de Application.</summary>
public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
}

public static class CategoryRequestMappings
{
    // Argumentos NOMBRADOS: no depende del orden; un campo mal escrito es error de compilación.
    public static CreateCategoryCommand ToCreateCommand(this CategoryRequest r) =>
        new(Name: r.Name, Description: r.Description, PlatformId: r.PlatformId);

    public static UpdateCategoryCommand ToUpdateCommand(this CategoryRequest r, Guid id) =>
        new(Id: id, Name: r.Name, Description: r.Description, PlatformId: r.PlatformId);
}
