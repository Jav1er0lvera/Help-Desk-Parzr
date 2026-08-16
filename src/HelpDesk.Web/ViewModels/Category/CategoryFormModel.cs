namespace HelpDesk.Web.ViewModels.Category;

/// <summary>
/// Datos que ENTRAN desde el modal de crear/editar (tipo "Request", CONCEPTOS #4).
/// Es un tipo del Web: lo bindea el controller y lo consume el service, que lo traduce
/// al DTO del SDK. Así el controller nunca toca <c>HelpDesk.SDK</c>.
/// </summary>
public class CategoryFormModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
}
