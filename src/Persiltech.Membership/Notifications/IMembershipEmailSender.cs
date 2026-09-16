namespace Persiltech.Membership.Notifications;

/// <summary>
/// Puerto de salida por el que el paquete entrega los avisos que hay que enviar por correo.
/// </summary>
/// <remarks>
/// Lo implementa y lo registra el <em>consumidor</em>: el paquete no trae ninguna
/// implementación ni registra una de reserva, porque una que no enviara nada convertiría un
/// olvido de configuración en un fallo silencioso.
/// <para>
/// El paquete no redacta el mensaje. Entrega los datos y el testigo, y quien compone el
/// asunto, el cuerpo y la URL de vuelta es el consumidor, que es el único que conoce el
/// patrón de ruta de la pantalla que recibe ese testigo.
/// </para>
/// </remarks>
public interface IMembershipEmailSender
{
    /// <summary>
    /// Envía la confirmación del correo de una cuenta recién registrada.
    /// </summary>
    /// <param name="message">Datos del aviso, con el testigo de confirmación.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la petición.</param>
    /// <returns>La tarea que representa el envío.</returns>
    Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Envía el aviso de reinicio de una contraseña olvidada.
    /// </summary>
    /// <param name="message">Datos del aviso, con el testigo de reinicio.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la petición.</param>
    /// <returns>La tarea que representa el envío.</returns>
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Envía la confirmación de un cambio de correo, al correo nuevo.
    /// </summary>
    /// <param name="message">Datos del aviso, con el testigo de cambio.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la petición.</param>
    /// <returns>La tarea que representa el envío.</returns>
    Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Avisa de que la cuenta acaba de quedar bloqueada por intentos fallidos.
    /// </summary>
    /// <remarks>
    /// Es el único aviso que no responde a una acción de su destinatario: le llega precisamente
    /// porque <em>otro</em> pudo haber estado intentando entrar. Ver
    /// <see cref="AccountLockedMessage"/> para por qué esto va por correo y no en la respuesta
    /// del inicio de sesión.
    /// <para>
    /// El paquete lo invoca en el mismo momento en que salta el bloqueo, y <b>trata un fallo
    /// aquí como no crítico</b>: si el envío revienta, el inicio de sesión responde igual. Un
    /// servidor de correo caído no puede dejar sin autenticar a nadie.
    /// </para>
    /// </remarks>
    /// <param name="message">Datos del aviso, sin testigo alguno.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la petición.</param>
    /// <returns>La tarea que representa el envío.</returns>
    Task SendAccountLockedAsync(AccountLockedMessage message, CancellationToken cancellationToken);
}
