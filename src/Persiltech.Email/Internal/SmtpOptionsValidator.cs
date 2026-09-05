namespace Persiltech.Email.Internal;

/// <summary>
/// Valida las <see cref="SmtpOptions"/> al arrancar la aplicación.
/// </summary>
/// <remarks>
/// Acumula los fallos en lugar de devolver el primero: un despliegue mal configurado los ve
/// todos en el primer arranque, en vez de descubrirlos de uno en uno a base de reinicios.
/// </remarks>
internal sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add("Host es obligatorio.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add($"Port tiene que estar entre 1 y 65535, y es {options.Port}.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            failures.Add("FromAddress es obligatoria.");
        }
        else if (!MailboxAddress.TryParse(EmailAddressParsing.ParserOptions, options.FromAddress, out _))
        {
            failures.Add($"FromAddress no es una dirección de correo válida: '{options.FromAddress}'.");
        }

        if (!string.IsNullOrWhiteSpace(options.UserName) && string.IsNullOrEmpty(options.Password))
        {
            failures.Add("Password es obligatoria cuando se configura UserName.");
        }

        if (options.TimeoutInSeconds is < 1 or > 600)
        {
            failures.Add(
                $"TimeoutInSeconds tiene que estar entre 1 y 600, y es {options.TimeoutInSeconds}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
