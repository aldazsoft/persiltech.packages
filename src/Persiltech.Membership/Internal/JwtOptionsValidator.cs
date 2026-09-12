namespace Persiltech.Membership.Internal;

/// <summary>
/// Valida las <see cref="JwtOptions"/> al arrancar la aplicación.
/// </summary>
/// <remarks>
/// Acumula los fallos en lugar de devolver el primero: un despliegue mal configurado los ve
/// todos en el primer arranque, en vez de descubrirlos de uno en uno a base de reinicios.
/// <para>
/// La clave se mide en bytes UTF-8 y no en caracteres, que es lo que cuenta HMAC-SHA256. Una
/// contada por caracteres deja pasar cadenas de 32 letras que en realidad son menos bytes de
/// entropía de la que el algoritmo pide, y rechaza claves con acentos o emoji que sí llegan.
/// </para>
/// </remarks>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    /// <summary>Bytes mínimos de la clave de firma, que son los 256 bits de HMAC-SHA256.</summary>
    private const int MinimumKeyLengthInBytes = 32;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SecurityKey))
        {
            failures.Add("SecurityKey es obligatoria.");
        }
        else
        {
            var lengthInBytes = Encoding.UTF8.GetByteCount(options.SecurityKey);

            if (lengthInBytes < MinimumKeyLengthInBytes)
            {
                failures.Add(
                    $"SecurityKey tiene que ocupar al menos {MinimumKeyLengthInBytes} bytes, " +
                    $"que son los 256 bits que exige HMAC-SHA256, y ocupa {lengthInBytes}.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.ValidIssuer))
        {
            failures.Add("ValidIssuer es obligatorio: es lo que viaja en la reclamación 'iss'.");
        }

        if (string.IsNullOrWhiteSpace(options.ValidAudience))
        {
            failures.Add("ValidAudience es obligatoria: es lo que viaja en la reclamación 'aud'.");
        }

        if (options.ExpireInMinutes < 1)
        {
            failures.Add(
                $"ExpireInMinutes tiene que ser mayor que cero, y es {options.ExpireInMinutes}.");
        }

        if (options.RefreshTokenExpireInDays < 1)
        {
            failures.Add(
                "RefreshTokenExpireInDays tiene que ser mayor que cero, y es " +
                $"{options.RefreshTokenExpireInDays}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
