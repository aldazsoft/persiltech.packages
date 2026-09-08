namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Respuesta de la API ya interpretada: o el cuerpo, o los errores de validación.
/// </summary>
/// <typeparam name="T">Tipo del valor que devuelve la llamada cuando sale bien.</typeparam>
/// <remarks>
/// Existe para que un <c>400</c> con <c>ValidationProblemDetails</c> llegue a la pantalla con
/// sus campos, en lugar de como una excepción sin contexto. Toda llamada del cliente pasa por
/// aquí, así que es el único sitio donde se decide qué es un fallo.
/// </remarks>
public sealed class ApiResult<T>
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>();

    private ApiResult(T? value, IReadOnlyDictionary<string, string[]>? errors, HttpStatusCode statusCode)
    {
        Value = value;
        Errors = errors ?? NoErrors;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Cuerpo de la respuesta, o <see langword="null"/> si la llamada falló o no devolvía nada.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Errores de validación por campo. Vacío cuando la llamada salió bien.
    /// </summary>
    /// <remarks>
    /// Las claves son las que devuelve la API. La cadena vacía es la que usa el paquete
    /// servidor para los errores que no señalan un campo, como unas credenciales inválidas.
    /// </remarks>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Código de estado que devolvió la API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// <see langword="true"/> si no hubo errores.
    /// </summary>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>
    /// Todos los mensajes de error en una sola línea, para mostrarlos en un aviso.
    /// </summary>
    public string ErrorSummary => string.Join(" ", Errors.SelectMany(entry => entry.Value));

    /// <summary>
    /// Construye el resultado de una llamada correcta.
    /// </summary>
    /// <param name="value">Cuerpo de la respuesta, si lo hubo.</param>
    /// <param name="statusCode">Código de estado que devolvió la API.</param>
    /// <returns>El resultado.</returns>
    public static ApiResult<T> Success(T? value, HttpStatusCode statusCode) =>
        new(value, null, statusCode);

    /// <summary>
    /// Construye el resultado de una llamada fallida a partir de los errores por campo.
    /// </summary>
    /// <param name="errors">Errores por campo, tal como los devolvió la API.</param>
    /// <param name="statusCode">Código de estado que devolvió la API.</param>
    /// <returns>El resultado.</returns>
    /// <remarks>
    /// Si <paramref name="errors"/> llega vacío se sustituye por un mensaje de reserva: un
    /// resultado fallido sin ningún error se comportaría como uno correcto, porque
    /// <see cref="Succeeded"/> se calcula a partir de ellos.
    /// </remarks>
    public static ApiResult<T> Failure(
        IReadOnlyDictionary<string, string[]> errors,
        HttpStatusCode statusCode) =>
        new(default, errors.Count == 0 ? FallbackFor(statusCode) : errors, statusCode);

    /// <summary>
    /// Construye el resultado de una llamada fallida de la que solo se conoce el código.
    /// </summary>
    /// <param name="statusCode">Código de estado que devolvió la API.</param>
    /// <returns>El resultado, con un mensaje de reserva que nombra ese código.</returns>
    public static ApiResult<T> Failure(HttpStatusCode statusCode) =>
        new(default, FallbackFor(statusCode), statusCode);

    /// <summary>
    /// Construye el resultado de una llamada fallida con un único mensaje sin campo.
    /// </summary>
    /// <param name="message">Qué salió mal.</param>
    /// <param name="statusCode">Código de estado que devolvió la API.</param>
    /// <returns>El resultado.</returns>
    /// <remarks>
    /// Para los fallos que no vienen de la validación de un formulario —la API apagada, un
    /// origen sin CORS—, donde no hay campo al que apuntar.
    /// </remarks>
    public static ApiResult<T> Failure(string message, HttpStatusCode statusCode) =>
        new(default, new Dictionary<string, string[]> { [string.Empty] = [message] }, statusCode);

    private static IReadOnlyDictionary<string, string[]> FallbackFor(HttpStatusCode statusCode) =>
        new Dictionary<string, string[]>
        {
            [string.Empty] = [$"La petición falló con el código {(int)statusCode}."]
        };
}
