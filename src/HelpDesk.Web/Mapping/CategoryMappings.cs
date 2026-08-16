using HelpDesk.SDK.Categories;
using HelpDesk.SDK.Platforms;
using HelpDesk.Web.ViewModels.Category;

namespace HelpDesk.Web.Mapping;

/// <summary>
/// Frontera de traducción entre el Web y el SDK, en ambos sentidos:
/// <list type="bullet">
///   <item>Salida: <c>CategoryDto</c> (SDK) → <c>CategoryViewModel</c> (Web), resolviendo
///   el nombre de la plataforma (el DTO solo trae <c>PlatformId</c>) y normalizando la descripción.</item>
///   <item>Entrada: <c>CategoryFormModel</c> (Web) → <c>CategoryRequestDto</c> (SDK).</item>
/// </list>
/// Centralizar aquí el mapeo mantiene el controller sin tipos del SDK.
/// </summary>
public static class CategoryMappings
{
    // Salida: DTO (SDK) → ViewModel (Web)
    public static List<CategoryViewModel> MapToViewModel(
        this List<HelpDesk.SDK.Categories.CategoryDto> dtos, List<PlatformDto> platforms)
    {
        var platformNames = platforms.ToDictionary(p => p.Id, p => p.Name);

        return dtos.Select((HelpDesk.SDK.Categories.CategoryDto d) => new CategoryViewModel
        {
            Id = d.Id,
            Name = d.Name,
            PlatformId = d.PlatformId,
            PlatformName = d.PlatformId is Guid pid && platformNames.TryGetValue(pid, out var name)
                ? name
                : "—",
            Description = string.IsNullOrWhiteSpace(d.Description) ? "—" : d.Description,
            CreatedAt = d.CreatedAt,
        }).ToList();
    }

    // Entrada: Form (Web) → RequestDto (SDK)
    public static CategoryRequestDto ToRequestDto(this CategoryFormModel form) => new()
    {
        Name = form.Name,
        Description = form.Description,
        PlatformId = form.PlatformId,
    };

    // DTO (SDK) → Form (Web): datos crudos para prellenar el modal de edición.
    // A diferencia de MapToViewModel, NO normaliza la descripción a "—" (se está editando).
    public static CategoryFormModel ToFormModel(this HelpDesk.SDK.Categories.CategoryDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        PlatformId = dto.PlatformId,
    };
}
