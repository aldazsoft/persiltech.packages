namespace Persiltech.Membership.Sample.Senders;

/// <summary>
/// Implementación del puerto de SMS que registra el aviso en el log en lugar de enviarlo.
/// </summary>
internal sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : IMembershipSmsSender
{
    public Task SendPhoneChangeAsync(PhoneChangeMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Cambio de teléfono a {PhoneNumber}. Código: {Token}",
            message.PhoneNumber,
            message.Token);

        return Task.CompletedTask;
    }
}
