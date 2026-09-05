namespace Persiltech.UserServices;

/// <summary>
/// Adaptador de infraestructura que implementa <see cref="IUserService"/> leyendo la
/// identidad establecida por ASP.NET Core en <c>HttpContext.User</c>.
/// </summary>
/// <remarks>
/// Las propiedades se evalúan en cada lectura y nunca se cachean: la misma instancia puede
/// atender peticiones distintas, y es el <see cref="IHttpContextAccessor"/> quien resuelve
/// el contexto de la petición en curso.
/// </remarks>
/// <param name="httpContextAccessor">Accesor del contexto de la petición en curso.</param>
/// <exception cref="ArgumentNullException">
/// <paramref name="httpContextAccessor"/> es <see langword="null"/>.
/// </exception>
public sealed class HttpContextUserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    /// <summary>
    /// Reclamación de OpenID Connect que transporta el login del usuario.
    /// </summary>
    private const string PreferredUserNameClaimType = "preferred_username";

    /// <summary>
    /// Reclamación de OpenID Connect que transporta el nombre completo del usuario.
    /// </summary>
    private const string FullNameClaimType = "name";

    private readonly IHttpContextAccessor HttpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc/>
    public bool IsAuthenticated => AuthenticatedUser is not null;

    /// <inheritdoc/>
    /// <remarks>
    /// Toma la primera reclamación con valor no vacío entre <c>Identity.Name</c>,
    /// <c>preferred_username</c> y <see cref="ClaimTypes.Upn"/>.
    /// </remarks>
    public string? UserName
    {
        get
        {
            if (AuthenticatedUser is not { } user)
            {
                return null;
            }

            return NonEmpty(user.Identity?.Name)
                ?? NonEmpty(user.FindFirst(PreferredUserNameClaimType)?.Value)
                ?? NonEmpty(user.FindFirst(ClaimTypes.Upn)?.Value);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Toma la reclamación <c>name</c> y, si no aporta valor, compone el nombre a partir de
    /// <see cref="ClaimTypes.GivenName"/> y <see cref="ClaimTypes.Surname"/>.
    /// </remarks>
    public string? FullName
    {
        get
        {
            if (AuthenticatedUser is not { } user)
            {
                return null;
            }

            return NonEmpty(user.FindFirst(FullNameClaimType)?.Value) ?? ComposeFullName(user);
        }
    }

    /// <summary>
    /// Usuario de la petición en curso, o <see langword="null"/> si no hay contexto activo o
    /// su identidad no está autenticada.
    /// </summary>
    private ClaimsPrincipal? AuthenticatedUser
    {
        get
        {
            var user = HttpContextAccessor.HttpContext?.User;

            return user?.Identity?.IsAuthenticated == true ? user : null;
        }
    }

    /// <summary>
    /// Une el nombre y los apellidos con un espacio, omitiendo el que falte.
    /// </summary>
    /// <param name="user">Usuario del que se leen las reclamaciones.</param>
    /// <returns>El nombre compuesto, o <see langword="null"/> si ninguna aporta valor.</returns>
    private static string? ComposeFullName(ClaimsPrincipal user)
    {
        var givenName = NonEmpty(user.FindFirst(ClaimTypes.GivenName)?.Value);
        var surname = NonEmpty(user.FindFirst(ClaimTypes.Surname)?.Value);

        if (givenName is null)
        {
            return surname;
        }

        return surname is null ? givenName : $"{givenName} {surname}";
    }

    /// <summary>
    /// Descarta los valores ausentes y los formados solo por espacios en blanco.
    /// </summary>
    /// <param name="value">Valor de la reclamación leída.</param>
    /// <returns>El valor tal cual, o <see langword="null"/> si está en blanco.</returns>
    private static string? NonEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
