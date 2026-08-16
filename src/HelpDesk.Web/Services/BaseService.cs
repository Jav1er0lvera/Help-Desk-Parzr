using System.Runtime.CompilerServices;
using HelpDesk.Web.Common;
using Refit;

namespace HelpDesk.Web.Services;

/// <summary>
/// Clase base de los servicios del Web. Centraliza el <c>try/catch</c> + logging
/// alrededor de la "lógica feliz" de cada método, para no repetirlo (DRY).
/// Combina tres patrones: <b>Execute-Around Method</b> (<see cref="ExecuteAsync{T}"/> envuelve
/// la acción), <b>Template Method</b> (<see cref="ServiceName"/> lo rellena cada hijo) y
/// <b>Result</b> (devuelve <see cref="ServiceResult{T}"/> en vez de lanzar excepciones).
/// El logging va por <see cref="ILogger"/> (MS.Logging), así los eventos llegan al
/// dashboard de Aspire vía el provider de OpenTelemetry que ya registra ServiceDefaults.
/// </summary>
public abstract class BaseService
{
    private readonly ILogger _logger;

    protected BaseService(ILogger logger) => _logger = logger;

    /// <summary>Nombre del servicio concreto, para los logs. Lo rellena cada hijo.</summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Ejecuta <paramref name="action"/> (la "lógica feliz") dentro de un try/catch común.
    /// Traduce cualquier excepción a un <see cref="ServiceResult{T}"/> con un mensaje limpio
    /// para el usuario, mientras loguea el detalle técnico.
    /// </summary>
    /// <param name="action">La lógica que devuelve el resultado en caso de éxito.</param>
    /// <param name="apiErrorMessage">Mensaje al usuario si falla la API (<see cref="ApiException"/>).</param>
    /// <param name="webErrorMessage">Mensaje al usuario si falla el Web (excepción no controlada).</param>
    /// <param name="method">Nombre del método llamante; lo rellena el compilador.</param>
    protected async Task<ServiceResult<T>> ExecuteAsync<T>(
        Func<Task<ServiceResult<T>>> action,
        string apiErrorMessage,
        string webErrorMessage,
        [CallerMemberName] string method = "")
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)          // cancelación: NO es un error
        {
            _logger.LogWarning("{Service}.{Method} - operación cancelada.", ServiceName, method);
            return ServiceResult<T>.Fail(ErrorMessages.OperationCancelled);
        }
        catch (ApiException ex)                      // error del lado de la API
        {
            _logger.LogError("API - {Service}.{Method} falló. Status: {Status}. Detalle: {Detail}",
                ServiceName, method, ex.StatusCode, ex.Content ?? ex.Message);
            return ServiceResult<T>.Fail(apiErrorMessage);
        }
        catch (Exception ex)                         // error del lado del Web
        {
            _logger.LogError(ex, "WEB - {Service}.{Method} falló.", ServiceName, method);
            return ServiceResult<T>.Fail(webErrorMessage);
        }
    }
}
