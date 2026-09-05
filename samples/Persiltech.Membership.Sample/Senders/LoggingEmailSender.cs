namespace Persiltech.Membership.Sample.Senders;

/// <summary>
/// Implementación del puerto de correo que registra el aviso en el log en lugar de
/// enviarlo. El paquete no trae ninguna: componerla es del consumidor.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IMembershipEmailSender
{
    public Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Confirmación de correo para {Email}. Testigo: {Token}",
            message.Email,
            message.Token);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Reinicio de contraseña para {Email}. Testigo: {Token}",
            message.Email,
            message.Token);

        return Task.CompletedTask;
    }

    public Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Cambio de correo a {NewEmail}. Testigo: {Token}",
            message.NewEmail,
            message.Token);

        return Task.CompletedTask;
    }
}
