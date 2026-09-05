namespace Persiltech.Membership.Internal;

/// <summary>
/// Emite el token de acceso como un JSON Web Token firmado con HMAC-SHA256 sobre los bytes
/// UTF-8 de <see cref="JwtOptions.SecurityKey"/>.
/// </summary>
/// <param name="options">Opciones de emisión, leídas en cada llamada.</param>
internal sealed class JwtAccessTokenFactory(IOptions<JwtOptions> options) : IAccessTokenFactory
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <inheritdoc />
    public string Create(ApplicationUser user, IReadOnlyList<string> roles)
    {
        var jwtOptions = options.Value;
        var issuedAt = DateTime.UtcNow;

        var claims = new Dictionary<string, object>
        {
            [ClaimTypes.Name] = user.Email ?? string.Empty,
            ["Fullname"] = $"{user.FirstName} {user.LastName}"
        };

        if (roles.Count > 0)
        {
            claims[ClaimTypes.Role] = roles;
        }

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
}
