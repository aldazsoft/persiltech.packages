namespace Persiltech.Membership.Email.Internal;

/// <summary>
/// Valida las <see cref="MembershipEmailOptions"/> al arrancar la aplicación.
/// </summary>
/// <remarks>
/// Acumula los fallos en lugar de devolver el primero: un despliegue mal configurado los ve
/// todos en el primer arranque, en vez de descubrirlos de uno en uno a base de reinicios.
/// </remarks>
internal sealed partial class MembershipEmailOptionsValidator : IValidateOptions<MembershipEmailOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MembershipEmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BrandName))
        {
            failures.Add("BrandName es obligatorio.");
        }

        ValidateAbsoluteUrl(failures, nameof(options.ClientBaseUrl), options.ClientBaseUrl, isRequired: true);
        ValidateAbsoluteUrl(failures, nameof(options.LogoUrl), options.LogoUrl, isRequired: false);

        ValidatePath(failures, nameof(options.EmailConfirmationPath), options.EmailConfirmationPath);
        ValidatePath(failures, nameof(options.PasswordResetPath), options.PasswordResetPath);
        ValidatePath(failures, nameof(options.EmailChangePath), options.EmailChangePath);

        if (!ColorPattern().IsMatch(options.PrimaryColor))
        {
            failures.Add(
                $"PrimaryColor tiene que ser un color hexadecimal (#rgb o #rrggbb), y es '{options.PrimaryColor}'.");
        }

        if (!string.IsNullOrWhiteSpace(options.SupportEmail) &&
            !MailAddress.TryCreate(options.SupportEmail, out _))
        {
            failures.Add($"SupportEmail no es una dirección de correo válida: '{options.SupportEmail}'.");
        }

        if (!string.IsNullOrWhiteSpace(options.TemplatesDirectory) &&
            !Directory.Exists(options.TemplatesDirectory))
        {
            failures.Add(
                $"TemplatesDirectory apunta a un directorio que no existe: '{options.TemplatesDirectory}'.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex ColorPattern();

    private static void ValidateAbsoluteUrl(List<string> failures, string memberName, string? value, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (isRequired)
            {
                failures.Add($"{memberName} es obligatoria.");
            }

            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"{memberName} tiene que ser una URL absoluta http o https, y es '{value}'.");
        }
    }

    private static void ValidatePath(List<string> failures, string memberName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{memberName} es obligatoria: sin ella el aviso no llevaría enlace de vuelta.");
        }
    }
}
