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
public sealed class MembershipAuthStateProvider(TokenStore tokens, MembershipApiClient api) : AuthenticationStateProvider
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

        if (claims.Count == 0)
        {
            await tokens.ClearAsync();

            return Anonymous;
        }

        // Un token caducado no es el final de la sesión: para eso está el testigo de
        // renovación. Solo si la renovación falla se cierra.
        if (IsExpired(claims))
        {
            claims = await RenewAsync();

            if (claims.Count == 0)
            {
                await tokens.ClearAsync();

                return Anonymous;
            }
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
    }

    private async Task<List<Claim>> RenewAsync()
    {
        var refreshToken = await tokens.GetMembershipRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return [];
        }

        var renewed = await api.RefreshAsync(new RefreshTokenRequest(refreshToken));

        if (!renewed.Succeeded || renewed.Value is null)
        {
            return [];
        }

        // Se guarda sin pasar por SignInAsync: ese notifica el cambio de estado, y aquí ya
        // estamos dentro del cálculo de ese mismo estado.
        await tokens.SetAsync(renewed.Value.AccessToken, renewed.Value.RefreshToken);

        return ReadClaims(renewed.Value.AccessToken);
    }

    /// <summary>
    /// Avisa a la interfaz de que hay una sesión nueva.
    /// </summary>
    /// <param name="accessToken">Token recién emitido.</param>
    /// <param name="refreshToken">Testigo con el que se renovará la sesión.</param>
    /// <returns>La tarea que representa el cambio de estado.</returns>
    public async Task SignInAsync(string accessToken, string refreshToken)
    {
        await tokens.SetAsync(accessToken, refreshToken);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Cierra la sesión y avisa a la interfaz.
    /// </summary>
    /// <returns>La tarea que representa el cierre.</returns>
    /// <remarks>
    /// Avisa primero al servidor para que revoque el testigo de renovación: borrarlo solo
    /// del navegador lo dejaría vivo y utilizable por quien lo hubiera copiado.
    /// </remarks>
    public async Task SignOutAsync()
    {
        var refreshToken = await tokens.GetMembershipRefreshTokenAsync();

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await api.LogoutAsync(new RefreshTokenRequest(refreshToken));
        }

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
