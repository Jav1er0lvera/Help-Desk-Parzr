namespace HelpDesk.Web.Common;

/// <summary>
/// Mensajes de usuario centralizados para la capa de servicio del Web.
/// Se muestran al usuario (limpios, sin detalles técnicos); el detalle real va al log.
/// Centralizarlos evita textos duplicados/inconsistentes repartidos por los servicios.
/// </summary>
public static class ErrorMessages
{
    // Genéricos
    public const string OperationCancelled = "La operación fue cancelada.";
    public const string ApiError = "No se pudo completar la operación en el servidor. Inténtalo de nuevo.";
    public const string WebError = "Ocurrió un error inesperado. Inténtalo de nuevo.";

    // Categorías (slice de referencia — PR 2)
    public const string ApiCategoriesListError = "No se pudieron obtener las categorías desde el servidor.";
    public const string WebCategoriesListError = "Ocurrió un error al cargar las categorías.";

    // Categorías — crear
    public const string ApiCategoriesCreateError = "No se pudo crear la categoría en el servidor.";
    public const string WebCategoriesCreateError = "Ocurrió un error al crear la categoría.";

    // Categorías — actualizar
    public const string ApiCategoriesUpdateError = "No se pudo actualizar la categoría en el servidor.";
    public const string WebCategoriesUpdateError = "Ocurrió un error al actualizar la categoría.";

    // Categorías — eliminar
    public const string ApiCategoriesDeleteError = "No se pudo eliminar la categoría en el servidor.";
    public const string WebCategoriesDeleteError = "Ocurrió un error al eliminar la categoría.";
}
