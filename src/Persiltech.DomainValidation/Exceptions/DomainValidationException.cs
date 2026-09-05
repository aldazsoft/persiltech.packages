namespace Persiltech.DomainValidation.Exceptions;

/// <summary>
/// Excepción que señala que una entidad no cumple sus reglas de negocio.
/// </summary>
public class DomainValidationException : Exception
{
    /// <summary>
    /// Errores que produjo la validación, o <see langword="null"/> si la excepción se
    /// construyó sin ellos.
    /// </summary>
    public IReadOnlyList<SpecificationError>? Errors { get; }

    /// <summary>
    /// Inicializa una excepción sin mensaje ni errores.
    /// </summary>
    public DomainValidationException() { }

    /// <summary>
    /// Inicializa una excepción con el mensaje indicado.
    /// </summary>
    /// <param name="message">Mensaje que describe el error.</param>
    public DomainValidationException(string message) : base(message) { }

    /// <summary>
    /// Inicializa una excepción con el mensaje indicado y la excepción que la originó.
    /// </summary>
    /// <param name="message">Mensaje que describe el error.</param>
    /// <param name="innerException">Excepción que originó esta.</param>
    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Inicializa una excepción con los errores de validación que la provocaron.
    /// </summary>
    /// <param name="errors">Errores producidos por la validación.</param>
    /// <param name="message">Mensaje que describe el error, opcional.</param>
    public DomainValidationException(
        IEnumerable<SpecificationError> errors, string? message = null)
        : base(message)
    {
        Errors = [.. errors];
    }
}
