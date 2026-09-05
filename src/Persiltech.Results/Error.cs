namespace Persiltech.Results;

/// <summary>
/// Un fallo concreto, con su mensaje y un código opcional que permite distinguirlo sin
/// comparar cadenas.
/// </summary>
/// <param name="code">
/// Código estable del fallo, o <see langword="null"/> si no lo tiene. Es lo que conviene mirar
/// para decidir, porque el mensaje puede estar traducido.
/// </param>
/// <param name="message">Mensaje que describe el fallo.</param>
public class Error(string? code, string message)
{
    /// <summary>
    /// Código estable del fallo, o <see langword="null"/> si se construyó solo con mensaje.
    /// </summary>
    public string? Code => code;

    /// <summary>
    /// Mensaje que describe el fallo.
    /// </summary>
    public string Message => message;

    /// <summary>
    /// Crea un fallo sin código, solo con su mensaje.
    /// </summary>
    /// <param name="message">Mensaje que describe el fallo.</param>
    public Error(string message) : this(null, message)
    { }
}
