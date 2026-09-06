namespace Persiltech.Membership.Blazor.Sample.Services;

/// <summary>
/// Guarda el token de acceso en el almacenamiento local del navegador.
/// </summary>
/// <remarks>
/// En una aplicación real conviene sopesar dónde vive el token: el almacenamiento local
/// sobrevive al cierre de la pestaña y es legible desde cualquier script de la página, así
/// que una vulnerabilidad de XSS lo expone. Se usa aquí por ser lo más simple de leer en un
/// ejemplo; una cookie <c>HttpOnly</c> emitida por un backend propio es más segura.
/// </remarks>
public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "persiltech.membership.accessToken";
    private const string MembershipRefreshKey = "persiltech.membership.refreshToken";
    private const string OAuthKey = "persiltech.oauth.accessToken";
    private const string RefreshKey = "persiltech.oauth.refreshToken";

    private string? Cached;
    private bool Loaded;

    /// <summary>
    /// Token de acceso actual, o <see langword="null"/> si no hay sesión.
    /// </summary>
    /// <returns>El token guardado.</returns>
    public async ValueTask<string?> GetAsync()
    {
        if (!Loaded)
        {
            Cached = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            Loaded = true;
        }

        return Cached;
    }

    /// <summary>
    /// Guarda el par de testigos recién emitido.
    /// </summary>
    /// <param name="accessToken">Token de acceso.</param>
    /// <param name="refreshToken">Testigo con el que se renovará la sesión.</param>
    /// <returns>La tarea que representa el guardado.</returns>
    public async ValueTask SetAsync(string accessToken, string refreshToken)
    {
        Cached = accessToken;
        Loaded = true;

        await js.InvokeVoidAsync("localStorage.setItem", Key, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", MembershipRefreshKey, refreshToken);
    }

    /// <summary>
    /// Testigo de renovación de la sesión de Membership, si lo hay.
    /// </summary>
    /// <returns>El testigo guardado.</returns>
    public ValueTask<string?> GetMembershipRefreshTokenAsync() =>
        js.InvokeAsync<string?>("localStorage.getItem", MembershipRefreshKey);

    /// <summary>
    /// Token de acceso emitido por el servidor de OAuth.
    /// </summary>
    /// <remarks>
    /// Se guarda aparte del de Membership a propósito: los dos son JWT, pero los firma
    /// distinto emisor y con distinta clave. El esquema JwtBearer del sample valida el del
    /// paquete base, así que el de OAuth no le vale — un despliegue real configuraría el
    /// servidor de recursos para aceptar el emisor que corresponda.
    /// </remarks>
    /// <returns>El token de OAuth guardado.</returns>
    public ValueTask<string?> GetOAuthTokenAsync() =>
        js.InvokeAsync<string?>("localStorage.getItem", OAuthKey);

    /// <summary>
    /// Guarda los testigos que devuelve el servidor de OAuth.
    /// </summary>
    /// <param name="accessToken">Token de acceso de OAuth.</param>
    /// <param name="refreshToken">Testigo de renovación.</param>
    /// <returns>La tarea que representa el guardado.</returns>
    public async ValueTask SetOAuthTokensAsync(string accessToken, string? refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", OAuthKey, accessToken);

        if (refreshToken is not null)
        {
            await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
        }
    }

    /// <summary>
    /// Testigo de renovación guardado, si lo hay.
    /// </summary>
    /// <returns>El testigo de renovación.</returns>
    public ValueTask<string?> GetRefreshTokenAsync() =>
        js.InvokeAsync<string?>("localStorage.getItem", RefreshKey);

    /// <summary>
    /// Borra la sesión.
    /// </summary>
    /// <returns>La tarea que representa el borrado.</returns>
    public async ValueTask ClearAsync()
    {
        Cached = null;
        Loaded = true;

        await js.InvokeVoidAsync("localStorage.removeItem", Key);
        await js.InvokeVoidAsync("localStorage.removeItem", MembershipRefreshKey);
        await js.InvokeVoidAsync("localStorage.removeItem", OAuthKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }
}
