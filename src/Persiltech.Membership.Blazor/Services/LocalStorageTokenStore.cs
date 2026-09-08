namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Guarda los testigos en el <c>localStorage</c> del navegador.
/// </summary>
/// <remarks>
/// Es la implementación por defecto: la única que funciona sin que el consumidor tenga un
/// backend propio. Ver <see cref="IMembershipTokenStore"/> para lo que eso implica.
/// <para>
/// <b>No cachea en memoria lo leído</b>, aunque el estado de autenticación se consulte en
/// cada navegación. La tentación es evidente y el fallo que provoca no: <c>IHttpClientFactory</c>
/// resuelve los <see cref="DelegatingHandler"/> en su propio ámbito, así que el manejador que
/// firma recibe una instancia distinta de la que usa la interfaz. Con caché, esa instancia
/// aprende «no hay sesión» en la primera petición —la de autenticarse, cuando todavía no la
/// hay— y se queda con esa respuesta para siempre: todo lo que venga después sale sin firmar.
/// El almacenamiento del navegador es el único estado compartido de verdad, y por eso se
/// consulta siempre.
/// </para>
/// </remarks>
/// <param name="jsRuntime">Puente con el navegador.</param>
public sealed class LocalStorageTokenStore(IJSRuntime jsRuntime) : IMembershipTokenStore
{
    private const string AccessTokenKey = "persiltech.membership.accessToken";
    private const string RefreshTokenKey = "persiltech.membership.refreshToken";

    /// <inheritdoc />
    public async ValueTask<MembershipTokens?> GetAsync()
    {
        var accessToken = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

        // Sin token de acceso no hay sesión que reconstruir, aunque quedara un testigo de
        // renovación suelto de una limpieza a medias.
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var refreshToken = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

        return new MembershipTokens(accessToken, refreshToken ?? string.Empty);
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(MembershipTokens tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, tokens.AccessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, tokens.RefreshToken);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }
}
