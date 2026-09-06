namespace Persiltech.Membership.Blazor.Sample.Services;

/// <summary>
/// Deriva el estado de autenticación de las reclamaciones del JWT que emite el paquete.
/// </summary>
/// <remarks>
/// El token se lee, no se valida: la firma la comprueba la API en cada petición, y hacerlo
/// también aquí no aportaría seguridad —el cliente ya está en manos de quien lo usa— pero sí
/// obligaría a repartir la clave. Lo que se lee sirve solo para decidir qué pinta la interfaz;
/// quien manda sobre el acceso real es la API.
/// </remarks>
public sealed class MembershipAuthStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        var claims = ReadClaims(token);

        if (claims.Count == 0 || IsExpired(claims))
        {
            await tokens.ClearAsync();

            return Anonymous;
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
    }

    /// <summary>
    /// Avisa a la interfaz de que hay una sesión nueva.
    /// </summary>
    /// <param name="accessToken">Token recién emitido.</param>
    /// <returns>La tarea que representa el cambio de estado.</returns>
    public async Task SignInAsync(string accessToken)
    {
        await tokens.SetAsync(accessToken);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Cierra la sesión y avisa a la interfaz.
    /// </summary>
    /// <returns>La tarea que representa el cierre.</returns>
    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    // El JWT viaja en tres partes separadas por puntos; la del medio es el cuerpo, en
    // base64url. Se decodifica a mano para no arrastrar una biblioteca de tokens al
    // navegador solo para leer unas reclamaciones.
    private static List<Claim> ReadClaims(string token)
    {
        var parts = token.Split('.');

        if (parts.Length != 3)
        {
            return [];
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                Decode(parts[1]));

            if (payload is null)
            {
                return [];
            }

            var claims = new List<Claim>();

            foreach (var (key, value) in payload)
            {
                var type = key switch
                {
                    // El paquete emite los nombres largos de ClaimTypes; el flujo de OAuth,
                    // los cortos del estándar. Se normalizan para que la interfaz no tenga
                    // que saber de qué flujo vino la sesión.
                    "sub" or "nameid" => ClaimTypes.NameIdentifier,
                    "name" or "unique_name" or "email" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    _ => key
                };

                if (value.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(value.EnumerateArray()
                        .Select(item => new Claim(type, item.ToString())));
                }
                else
                {
                    claims.Add(new Claim(type, value.ToString()));
                }
            }

            return claims;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsExpired(List<Claim> claims)
    {
        var expiry = claims.FirstOrDefault(claim => claim.Type == "exp")?.Value;

        return long.TryParse(expiry, out var seconds)
            && DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }

    private static byte[] Decode(string segment)
    {
        var value = segment.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(value.PadRight(value.Length + ((4 - (value.Length % 4)) % 4), '='));
    }
}
