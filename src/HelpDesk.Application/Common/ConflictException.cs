namespace HelpDesk.Application.Common;

/// <summary>Regla de negocio violada que debe traducirse a HTTP 409 (ver GlobalExceptionHandler en la API).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
