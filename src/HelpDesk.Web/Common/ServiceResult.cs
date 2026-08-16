namespace HelpDesk.Web.Common;

/// <summary>
/// Envoltorio de resultado (Result Pattern) que devuelve la capa de servicio.
/// Distingue tres estados que un <c>StatusCode(500)</c> mudo no puede:
/// <see cref="Success"/> (OK con datos), <see cref="Warning"/> (OK pero vacío)
/// y fallo (error real). Separa el detalle técnico (log) del mensaje al usuario.
/// </summary>
/// <typeparam name="T">Tipo del dato que transporta cuando la operación tiene éxito.</typeparam>
public class ServiceResult<T>
{
    /// <summary>Los datos de la operación. Null si fue advertencia o fallo.</summary>
    public T? Data { get; set; }

    /// <summary>OK con datos.</summary>
    public bool Success { get; set; }

    /// <summary>OK pero sin datos (p. ej. "aún no hay elementos"): no es un error.</summary>
    public bool Warning { get; set; }

    /// <summary>Mensaje principal para mostrar al usuario.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Mensajes adicionales (p. ej. varias validaciones).</summary>
    public List<string> Messages { get; set; } = [];

    /// <summary>Éxito con datos.</summary>
    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    /// <summary>OK pero sin datos: no es un error, solo una advertencia para el usuario.</summary>
    public static ServiceResult<T> Warn(string message) => new() { Warning = true, Message = message };

    /// <summary>Fallo con un mensaje limpio para el usuario (el detalle va al log).</summary>
    public static ServiceResult<T> Fail(string message) => new() { Success = false, Message = message };
}
