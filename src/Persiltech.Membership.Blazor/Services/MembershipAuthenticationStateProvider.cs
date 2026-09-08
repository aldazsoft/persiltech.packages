namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Deriva el estado de autenticación de las reclamaciones del token que emite la API.
/// </summary>
/// <remarks>
/// El token se lee, no se valida: la firma la comprueba la API en cada petición, y hacerlo
/// también aquí no aportaría seguridad —el cliente ya está en manos de quien lo usa— pero sí
/// obligaría a repartir la clave. Lo que se lee sirve para decidir qué pinta la interfaz;
/// quien manda sobre el acceso real es la API.
/// </remarks>
/// <param name="tokens">Almacén de los testigos de la sesión.</param>
/// <param name="apiClient">Cliente con el que se renueva.</param>
public sealed class MembershipAuthenticationStateProvider(
    IMembershipTokenStore tokens,
    IMembershipApiClient apiClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var stored = await tokens.GetAsync();

        if (stored is null)
        {
            return Anonymous;
        }

        var claims = ReadClaims(stored.AccessToken);

        if (claims.Count == 0)
        {
            await tokens.ClearAsync();

            return Anonymous;
        }

        // Un token caducado no es el final de la sesión: para eso está el testigo de
        // renovación. Solo si la renovación falla se cierra.
        if (IsExpired(claims))
        {
            claims = await RenewAsync(stored.RefreshToken);

            if (claims.Count == 0)
            {
                await tokens.ClearAsync();

                return Anonymous;
            }
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
    }

    /// <summary>
    /// Guarda la sesión recién abierta y avisa a la interfaz.
    /// </summary>
    /// <param name="sessionTokens">Testigos que devolvió la API.</param>
    /// <returns>La tarea que representa el cambio de estado.</returns>
    public async Task SignInAsync(MembershipTokens sessionTokens)
    {
        ArgumentNullException.ThrowIfNull(sessionTokens);

        await tokens.SetAsync(sessionTokens);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Cierra la sesión y avisa a la interfaz.
    /// </summary>
    /// <returns>La tarea que representa el cierre.</returns>
    /// <remarks>
    /// Avisa primero a la API para que revoque la familia del testigo: borrarlo solo del
    /// navegador lo dejaría vivo para quien lo hubiera copiado.
    /// </remarks>
    public async Task SignOutAsync()
    {
        var stored = await tokens.GetAsync();

        if (stored is not null && !string.IsNullOrWhiteSpace(stored.RefreshToken))
        {
            await apiClient.LogoutAsync(stored.RefreshToken);
        }

        await tokens.ClearAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private async Task<List<Claim>> RenewAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return [];
        }

        var renewed = await apiClient.RefreshAsync(refreshToken);

        if (!renewed.Succeeded || renewed.Value is null)
        {
            return [];
        }

        // Se guarda sin pasar por SignInAsync: ese notifica el cambio de estado, y aquí ya
        // estamos dentro del cálculo de ese mismo estado.
        await tokens.SetAsync(renewed.Value);

        return ReadClaims(renewed.Value.AccessToken);
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
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Decode(parts[1]));

            if (payload is null)
            {
                return [];
            }

            List<Claim> claims = [];

            foreach (var (key, value) in payload)
            {
                var type = Normalize(key);

                if (value.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(value.EnumerateArray().Select(item => new Claim(type, item.ToString())));
                }
                else
                {
                    claims.Add(new Claim(type, value.ToString()));
                }
            }

            return claims;
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return [];
        }
    }

    // El paquete servidor emite los nombres largos de ClaimTypes; otros emisores usan los
    // cortos del estándar. Se normalizan para que la interfaz no tenga que saber cuál llegó.
    private static string Normalize(string key) => key switch
    {
        "sub" or "nameid" => ClaimTypes.NameIdentifier,
        "name" or "unique_name" or "email" => ClaimTypes.Name,
        "role" => ClaimTypes.Role,
        _ => key
    };

    private static bool IsExpired(List<Claim> claims)
    {
        var expiry = claims.FirstOrDefault(claim => claim.Type == "exp")?.Value;

        return long.TryParse(expiry, out var seconds)
            && DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }

    private static byte[] Decode(string segment)
    {
        var value = segment.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(
            value.PadRight(value.Length + ((4 - (value.Length % 4)) % 4), '='));
    }
}
