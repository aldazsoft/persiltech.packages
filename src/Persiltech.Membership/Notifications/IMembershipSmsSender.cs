namespace Persiltech.Membership.Notifications;

/// <summary>
/// Puerto de salida por el que el paquete entrega los avisos que hay que enviar por SMS.
/// </summary>
/// <remarks>
/// Lo implementa y lo registra el consumidor, igual que
/// <see cref="IMembershipEmailSender"/>. Solo hace falta si se montan los endpoints de
/// teléfono.
/// </remarks>
public interface IMembershipSmsSender
{
    /// <summary>
    /// Envía el código de confirmación de un cambio de teléfono.
    /// </summary>
    /// <param name="message">Datos del aviso, con el código de confirmación.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la petición.</param>
    /// <returns>La tarea que representa el envío.</returns>
    Task SendPhoneChangeAsync(PhoneChangeMessage message, CancellationToken cancellationToken);
}
