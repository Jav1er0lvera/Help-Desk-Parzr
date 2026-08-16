namespace HelpDesk.Web.ViewModels.Category;

/// <summary>
/// Datos de una categoría listos para mostrar en la vista. Todo viene ya resuelto
/// (p. ej. <see cref="PlatformName"/> y <see cref="Description"/>); la vista solo pinta.
/// </summary>
public class CategoryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Id de la plataforma; se usa para prellenar el modal de edición.</summary>
    public Guid? PlatformId { get; set; }

    /// <summary>Nombre de la plataforma, resuelto en el mapper (cae a "—" si no hay match).</summary>
    public string PlatformName { get; set; } = "—";

    /// <summary>Descripción normalizada en el mapper (cae a "—" si viene vacía).</summary>
    public string Description { get; set; } = "—";

    public DateTime CreatedAt { get; set; }
}
