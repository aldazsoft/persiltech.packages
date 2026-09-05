namespace Persiltech.Email;

/// <summary>
/// Mensaje a enviar, ya redactado.
/// </summary>
/// <remarks>
/// El remitente no viaja aquí: vive en <see cref="SmtpOptions"/>, porque es propiedad de la
/// cuenta con la que se conecta y no de cada envío.
/// </remarks>
public sealed record EmailMessage
{
    /// <summary>
    /// Destinatario del mensaje.
    /// </summary>
    /// <remarks>
    /// Admite la dirección sola (<c>juan@example.com</c>) o acompañada del nombre visible
    /// (<c>Juan Pérez &lt;juan@example.com&gt;</c>).
    /// </remarks>
    public required string To { get; init; }

    /// <summary>
    /// Asunto del mensaje.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Cuerpo del mensaje en HTML.
    /// </summary>
    public required string HtmlBody { get; init; }

    /// <summary>
    /// Cuerpo alternativo en texto plano, para los clientes que no muestran HTML.
    /// </summary>
    /// <remarks>
    /// Si es <see langword="null"/>, el mensaje viaja solo con la parte HTML.
    /// </remarks>
    public string? TextBody { get; init; }
}
