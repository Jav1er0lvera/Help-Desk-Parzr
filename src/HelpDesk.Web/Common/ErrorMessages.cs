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
}
