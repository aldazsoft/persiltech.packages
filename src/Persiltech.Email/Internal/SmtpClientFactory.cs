namespace Persiltech.Email.Internal;

/// <summary>
/// Crea el cliente SMTP de MailKit.
/// </summary>
internal sealed class SmtpClientFactory : ISmtpClientFactory
{
    /// <inheritdoc />
    public ISmtpClient Create() => new SmtpClient();
}
