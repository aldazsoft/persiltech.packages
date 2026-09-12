namespace Persiltech.Membership.Blazor;

/// <summary>
/// Valida las <see cref="MembershipApiOptions"/>.
/// </summary>
/// <remarks>
/// Implementa <see cref="IValidateOptions{TOptions}"/> como el resto de la casa, pero no se
/// registra para <c>ValidateOnStart</c>: en Blazor WebAssembly no hay host que arranque
/// servicios, así que esa validación nunca correría y una dirección base mal puesta se
/// descubriría en la primera petición. Lo invoca
/// <see cref="DependencyInjection.AddMembershipBlazor"/> al registrar, que es el momento más
/// temprano que existe en este modelo de alojamiento.
/// </remarks>
internal sealed class MembershipApiOptionsValidator : IValidateOptions<MembershipApiOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MembershipApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseAddress))
        {
            failures.Add($"{nameof(MembershipApiOptions.BaseAddress)} es obligatoria.");
        }
        // Se exige el esquema y no solo que sea absoluta: en Unix, '/api' parsea como URI
        // absoluta —'file:///api'— y en Windows no. Comprobando http o https, una dirección
        // mal puesta se rechaza igual en las dos, y no acaba en un HttpClient apuntando al
        // sistema de archivos.
        else if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var baseAddress)
            || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add(
                $"{nameof(MembershipApiOptions.BaseAddress)} tiene que ser una URL absoluta " +
                $"http o https. Se recibió: '{options.BaseAddress}'.");
        }

        // Las rutas se concatenan a la dirección base, así que una que empiece por '/' se
        // comería el camino de la base y dejaría las peticiones en la raíz del dominio.
        foreach (var (property, path) in Paths(options))
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                failures.Add($"{property} es obligatoria.");
            }
            else if (path.StartsWith('/'))
            {
                failures.Add(
                    $"{property} es relativa a BaseAddress, así que no puede empezar por '/'. " +
                    $"Se recibió: '{path}'.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static Dictionary<string, string> Paths(MembershipApiOptions options) => new()
    {
        [nameof(options.LoginPath)] = options.LoginPath,
        [nameof(options.RegisterPath)] = options.RegisterPath,
        [nameof(options.RefreshPath)] = options.RefreshPath,
        [nameof(options.LogoutPath)] = options.LogoutPath,
        [nameof(options.PasswordPath)] = options.PasswordPath,
        [nameof(options.EmailPath)] = options.EmailPath,
        [nameof(options.PhonePath)] = options.PhonePath,
        [nameof(options.ProfilePath)] = options.ProfilePath,
        [nameof(options.TwoFactorPath)] = options.TwoFactorPath,
        [nameof(options.RolesPath)] = options.RolesPath,
        [nameof(options.UsersPath)] = options.UsersPath
    };
}
