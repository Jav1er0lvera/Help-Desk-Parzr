namespace HelpDesk.Application.Categories;

/// <summary>
/// DTO de respuesta de la API para una categoría. Vive en Application (no en Domain).
/// Shape compatible con el CategoryDto del SDK; omite IsDeleted (bandera interna).
/// </summary>
public sealed record CategoryDto(Guid Id, string Name, string? Description, Guid? PlatformId, DateTime CreatedAt);
