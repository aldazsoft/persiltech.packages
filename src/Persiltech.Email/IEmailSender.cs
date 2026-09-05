namespace Persiltech.Email;

/// <summary>
/// Puerto de envío de correo. Es el tipo que el consumidor resuelve del contenedor.
/// </summary>
/// <remarks>
/// El paquete transporta, no redacta: recibe el asunto y el cuerpo ya compuestos. Quien
/// elige plantilla, formato, idioma y enlaces es el consumidor, que es el único que conoce
/// las rutas de su aplicación.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Entrega el mensaje al servidor de correo.
    /// </summary>
    /// <param name="message">Mensaje ya redactado.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>
    /// La tarea que representa el envío. Completa cuando el servidor acepta el mensaje.
    /// </returns>
    /// <remarks>
    /// La aceptación por parte del servidor no es la entrega al buzón del destinatario: lo
    /// que ocurra después del salto está fuera del alcance de SMTP.
    /// </remarks>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
