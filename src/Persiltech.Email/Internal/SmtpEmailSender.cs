namespace Persiltech.Email.Internal;

/// <summary>
/// Envía el mensaje por SMTP con MailKit.
/// </summary>
/// <remarks>
/// Cada envío abre su propia conexión, la cierra y desecha el cliente: el cliente SMTP de
/// MailKit no es seguro para uso concurrente, y mantener una conexión compartida exigiría un
/// pool con sincronización que este paquete no ha demostrado necesitar.
/// </remarks>
/// <param name="clientFactory">Crea el cliente de cada envío.</param>
/// <param name="options">Opciones de conexión, leídas en cada llamada.</param>
internal sealed class SmtpEmailSender(ISmtpClientFactory clientFactory, IOptions<SmtpOptions> options) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var smtpOptions = options.Value;

        // Se compone antes de conectar: un destinatario inválido no merece abrir un socket,
        // y así el error llega de inmediato en lugar de tras el saludo del servidor.
        var mimeMessage = CreateMimeMessage(message, smtpOptions);

        using var client = clientFactory.Create();

        client.Timeout = (int)TimeSpan.FromSeconds(smtpOptions.TimeoutInSeconds).TotalMilliseconds;

        await client.ConnectAsync(
            smtpOptions.Host,
            smtpOptions.Port,
            ToSecureSocketOptions(smtpOptions.Security),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtpOptions.UserName))
        {
            await client.AuthenticateAsync(
                smtpOptions.UserName,
                smtpOptions.Password ?? string.Empty,
                cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static MimeMessage CreateMimeMessage(EmailMessage message, SmtpOptions smtpOptions)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(smtpOptions.FromDisplayName ?? string.Empty, smtpOptions.FromAddress));
        mimeMessage.To.Add(ParseRecipient(message.To));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.HtmlBody };

        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            bodyBuilder.TextBody = message.TextBody;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    private static MailboxAddress ParseRecipient(string to) =>
        MailboxAddress.TryParse(EmailAddressParsing.ParserOptions, to, out var recipient)
            ? recipient
            : throw new ArgumentException(
                $"El destinatario no es una dirección de correo válida: '{to}'.",
                nameof(EmailMessage.To));

    private static SecureSocketOptions ToSecureSocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.Auto
    };
}
