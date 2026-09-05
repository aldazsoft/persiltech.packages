namespace Persiltech.Membership.Email.Internal;

/// <summary>
/// Redacta los avisos de Persiltech.Membership con las plantillas del paquete y los entrega
/// por el puerto de transporte de Persiltech.Email.
/// </summary>
/// <param name="emailSender">Transporte que aporta el consumidor.</param>
/// <param name="templateRenderer">Compositor de las plantillas.</param>
/// <param name="options">Marca y rutas de la aplicación cliente.</param>
internal sealed class TemplatedMembershipEmailSender(
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<MembershipEmailOptions> options) : IMembershipEmailSender
{
    /// <inheritdoc />
    public Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(
            "EmailConfirmation",
            message.Email,
            message.FirstName,
            message.LastName,
            BuildUrl(options.Value.EmailConfirmationPath, "email", message.Email, message.Token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(
            "PasswordReset",
            message.Email,
            message.FirstName,
            message.LastName,
            BuildUrl(options.Value.PasswordResetPath, "email", message.Email, message.Token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(
            "EmailChange",
            message.NewEmail,
            message.FirstName,
            message.LastName,
            BuildUrl(options.Value.EmailChangePath, "newEmail", message.NewEmail, message.Token),
            cancellationToken);
    }

    private Task SendAsync(
        string templateName,
        string to,
        string firstName,
        string lastName,
        string actionUrl,
        CancellationToken cancellationToken)
    {
        var rendered = templateRenderer.Render(templateName, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["FirstName"] = firstName,
            ["LastName"] = lastName,
            ["FullName"] = $"{firstName} {lastName}".Trim(),
            ["Email"] = to,
            ["ActionUrl"] = actionUrl
        });

        return emailSender.SendAsync(
            new EmailMessage
            {
                To = to,
                Subject = rendered.Subject,
                HtmlBody = rendered.HtmlBody,
                TextBody = rendered.TextBody
            },
            cancellationToken);
    }

    private string BuildUrl(string path, string parameterName, string parameterValue, string token)
    {
        var baseUrl = options.Value.ClientBaseUrl.TrimEnd('/');
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";

        return $"{baseUrl}{normalizedPath}" +
            $"?{parameterName}={Uri.EscapeDataString(parameterValue)}" +
            $"&token={Uri.EscapeDataString(token)}";
    }
}
