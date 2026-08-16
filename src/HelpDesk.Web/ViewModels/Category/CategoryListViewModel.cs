using HelpDesk.SDK.Platforms;

namespace HelpDesk.Web.ViewModels.Category;

/// <summary>
/// ViewModel de la pantalla de Categorías: la lista a pintar y las plataformas
/// disponibles para el <c>&lt;select&gt;</c> del modal de crear/editar.
/// </summary>
public class CategoryListViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = [];
    public List<PlatformDto> Platforms { get; set; } = [];
}
