namespace Persiltech.Membership.Internal;

/// <summary>
/// Emite el token de acceso como un JSON Web Token firmado con HMAC-SHA256 sobre los bytes
/// UTF-8 de <see cref="JwtOptions.SecurityKey"/>.
/// </summary>
/// <param name="options">Opciones de emisión, leídas en cada llamada.</param>
/// <param name="claimsProviders">
/// Aportaciones del consumidor. Vacío si no registró ninguna, que es el caso corriente.
/// </param>
internal sealed class JwtAccessTokenFactory(
    IOptions<JwtOptions> options,
    IEnumerable<IAccessTokenClaimsProvider> claimsProviders) : IAccessTokenFactory
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <summary>
    /// Reclamaciones que emite el paquete y que nadie de fuera puede sobrescribir.
    /// </summary>
    /// <remarks>
    /// Dejar que una aportación pisara <see cref="ClaimTypes.Role"/> sería regalar una vía de
    /// escalada de privilegios: quien pudiera registrar un proveedor podría concederse
    /// cualquier rol. Se corta ahí, y en voz alta.
    /// </remarks>
    private static readonly HashSet<string> ReservedClaims = new(StringComparer.Ordinal)
    {
        ClaimTypes.Name,
        ClaimTypes.Role,
        "Fullname"
    };

    /// <inheritdoc />
    public async Task<string> CreateAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        var jwtOptions = options.Value;
        var issuedAt = DateTime.UtcNow;

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [ClaimTypes.Name] = user.Email ?? string.Empty,
            ["Fullname"] = $"{user.FirstName} {user.LastName}"
        };

        if (roles.Count > 0)
        {
            claims[ClaimTypes.Role] = roles;
        }

        await AddProvidedClaimsAsync(claims, user, cancellationToken);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.ValidIssuer,
            Audience = jwtOptions.ValidAudience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.AddMinutes(jwtOptions.ExpireInMinutes),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return TokenHandler.CreateToken(descriptor);
    }

    private async Task AddProvidedClaimsAsync(
        Dictionary<string, object> claims,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        foreach (var provider in claimsProviders)
        {
            var provided = await provider.GetClaimsAsync(user, cancellationToken);

            if (provided is null)
            {
                continue;
            }

            foreach (var claim in provided)
            {
                if (string.IsNullOrWhiteSpace(claim.Key))
                {
                    throw new InvalidOperationException(
                        $"'{provider.GetType().Name}' devolvió una reclamación sin nombre.");
                }

                if (ReservedClaims.Contains(claim.Key))
                {
                    throw new InvalidOperationException(
                        $"'{provider.GetType().Name}' intenta sobrescribir la reclamación " +
                        $"'{claim.Key}', que emite el propio paquete. Usa otro nombre.");
                }

                // Dos proveedores que aportan el mismo nombre son un choque de configuración,
                // no una precedencia: el que ganara dependería del orden de registro.
                if (!claims.TryAdd(claim.Key, claim.Value))
                {
                    throw new InvalidOperationException(
                        $"'{provider.GetType().Name}' aporta la reclamación '{claim.Key}', que " +
                        "ya había aportado otro proveedor.");
                }
            }
        }
    }
}
