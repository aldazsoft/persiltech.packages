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
    private readonly List<AccountLockedMessage> AccountLockouts = [];

    /// <summary>
    /// Si el envío del aviso de bloqueo debe fallar, para comprobar que eso no tumba el
    /// inicio de sesión.
    /// </summary>
    internal bool FailAccountLocked { get; set; }

    /// <summary>Confirmaciones de correo entregadas.</summary>
    internal IReadOnlyList<EmailConfirmationMessage> Confirmations => EmailConfirmations;

    /// <summary>Reinicios de contraseña entregados.</summary>
    internal IReadOnlyList<PasswordResetMessage> Resets => PasswordResets;

    /// <summary>Cambios de correo entregados.</summary>
    internal IReadOnlyList<EmailChangeMessage> Changes => EmailChanges;

    /// <summary>Cambios de teléfono entregados.</summary>
    internal IReadOnlyList<PhoneChangeMessage> Phones => PhoneChanges;

    /// <summary>Avisos de bloqueo de cuenta entregados.</summary>
    internal IReadOnlyList<AccountLockedMessage> Lockouts => AccountLockouts;

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

    /// <inheritdoc />
    public Task SendAccountLockedAsync(AccountLockedMessage message, CancellationToken cancellationToken)
    {
        if (FailAccountLocked)
        {
            throw new InvalidOperationException("El servidor de correo no responde.");
        }

        AccountLockouts.Add(message);

        return Task.CompletedTask;
    }
}
