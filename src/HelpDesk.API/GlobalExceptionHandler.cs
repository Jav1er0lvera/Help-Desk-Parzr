using HelpDesk.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API;

/// <summary>Traduce excepciones de negocio conocidas a respuestas HTTP. Hoy: ConflictException → 409.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not ConflictException) return false;   // el resto sigue su curso

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto",
            Detail = exception.Message
        };
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
