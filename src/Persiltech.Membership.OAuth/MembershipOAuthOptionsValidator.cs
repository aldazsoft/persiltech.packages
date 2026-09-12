namespace Persiltech.Membership.OAuth;

/// <summary>
/// Valida las <see cref="MembershipOAuthOptions"/> al arrancar la aplicación.
/// </summary>
/// <remarks>
/// Acumula los fallos en lugar de devolver el primero: un despliegue mal configurado los ve
/// todos en el primer arranque, en vez de descubrirlos de uno en uno a base de reinicios.
/// <para>
/// Comprueba además que las rutas empiecen por <c>/</c> y no se repitan entre sí, que es lo
/// que una anotación no alcanza. Dos endpoints en la misma ruta arrancan sin protestar y
/// fallan al primer intento de autorizar, cuando ya cuesta relacionarlo con la causa.
/// </para>
/// </remarks>
internal sealed class MembershipOAuthOptionsValidator : IValidateOptions<MembershipOAuthOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MembershipOAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        var paths = new Dictionary<string, string>
        {
            [nameof(options.AuthorizationEndpointPath)] = options.AuthorizationEndpointPath,
            [nameof(options.TokenEndpointPath)] = options.TokenEndpointPath,
            [nameof(options.UserInfoEndpointPath)] = options.UserInfoEndpointPath,
            [nameof(options.EndSessionEndpointPath)] = options.EndSessionEndpointPath,
            [nameof(options.RevocationEndpointPath)] = options.RevocationEndpointPath,
            [nameof(options.LoginPath)] = options.LoginPath
        };

        foreach (var (property, path) in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                failures.Add($"{property} es obligatoria.");
            }
            else if (!path.StartsWith('/'))
            {
                failures.Add($"{property} tiene que empezar por '/', y es '{path}'.");
            }
        }

        var duplicated = paths
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .GroupBy(p => p.Value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicated)
        {
            failures.Add(
                $"'{group.Key}' está repetida en {string.Join(", ", group.Select(p => p.Key))}: " +
                "cada endpoint necesita la suya.");
        }

        if (string.IsNullOrWhiteSpace(options.InteractiveAuthenticationScheme))
        {
            failures.Add(
                "InteractiveAuthenticationScheme es obligatorio: es con lo que se comprueba la " +
                "sesión interactiva en el endpoint de autorización.");
        }

        if (options.AccessTokenLifetimeInMinutes < 1)
        {
            failures.Add(
                "AccessTokenLifetimeInMinutes tiene que ser mayor que cero, y es " +
                $"{options.AccessTokenLifetimeInMinutes}.");
        }

        if (options.RefreshTokenLifetimeInDays < 1)
        {
            failures.Add(
                "RefreshTokenLifetimeInDays tiene que ser mayor que cero, y es " +
                $"{options.RefreshTokenLifetimeInDays}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
