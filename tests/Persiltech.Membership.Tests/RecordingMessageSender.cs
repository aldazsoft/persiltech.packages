namespace Persiltech.Membership.Tests;

/// <summary>
/// Implementación de los puertos de salida que guarda los avisos en lugar de enviarlos.
/// </summary>
/// <remarks>
/// Es lo que permite a la prueba hacerse con el testigo que el paquete entrega, y con él
/// completar los flujos de confirmación y reinicio de extremo a extremo.
/// </remarks>
internal sealed class RecordingMessageSender : IMembershipEmailSender, IMembershipSmsSender
{
    private readonly List<EmailConfirmationMessage> EmailConfirmations = [];
    private readonly List<PasswordResetMessage> PasswordResets = [];
    private readonly List<EmailChangeMessage> EmailChanges = [];
    private readonly List<PhoneChangeMessage> PhoneChanges = [];

    /// <summary>Confirmaciones de correo entregadas.</summary>
    internal IReadOnlyList<EmailConfirmationMessage> Confirmations => EmailConfirmations;

    /// <summary>Reinicios de contraseña entregados.</summary>
    internal IReadOnlyList<PasswordResetMessage> Resets => PasswordResets;

    /// <summary>Cambios de correo entregados.</summary>
    internal IReadOnlyList<EmailChangeMessage> Changes => EmailChanges;

    /// <summary>Cambios de teléfono entregados.</summary>
    internal IReadOnlyList<PhoneChangeMessage> Phones => PhoneChanges;

    /// <inheritdoc />
    public Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken)
    {
        EmailConfirmations.Add(message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)
    {
        PasswordResets.Add(message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken)
    {
        EmailChanges.Add(message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPhoneChangeAsync(PhoneChangeMessage message, CancellationToken cancellationToken)
    {
        PhoneChanges.Add(message);

        return Task.CompletedTask;
    }
}
