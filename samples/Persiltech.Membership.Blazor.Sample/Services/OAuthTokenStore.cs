namespace Persiltech.Membership.Blazor.Sample.Services;

/// <summary>
/// Guarda los testigos que emite el servidor de OAuth, aparte de los de Membership.
/// </summary>
/// <remarks>
/// Se guardan aparte a propósito: los dos son JWT, pero los firma distinto emisor y con
/// distinta clave. El esquema JwtBearer del sample valida el del paquete base, así que el de
/// OAuth no le vale — un despliegue real configuraría el servidor de recursos para aceptar
/// el emisor que corresponda.
/// <para>
/// El paquete no trae este almacén porque no conoce el flujo de OAuth: eso lo aporta
/// <c>Persiltech.Membership.OAuth</c> en el servidor, y aquí lo compone el consumidor.
/// </para>
/// </remarks>
/// <param name="js">Puente con el navegador.</param>
public sealed class OAuthTokenStore(IJSRuntime js)
{
    private const string AccessKey = "persiltech.oauth.accessToken";
    private const string RefreshKey = "persiltech.oauth.refreshToken";

    /// <summary>
    /// Token de acceso emitido por el servidor de OAuth.
    /// </summary>
    /// <returns>El token guardado.</returns>
    public ValueTask<string?> GetOAuthTokenAsync() =>
        js.InvokeAsync<string?>("localStorage.getItem", AccessKey);

    /// <summary>
    /// Testigo de renovación emitido por el servidor de OAuth.
    /// </summary>
    /// <returns>El testigo guardado.</returns>
    public ValueTask<string?> GetRefreshTokenAsync() =>
        js.InvokeAsync<string?>("localStorage.getItem", RefreshKey);

    /// <summary>
    /// Guarda los testigos que devolvió el servidor de OAuth.
    /// </summary>
    /// <param name="accessToken">Token de acceso.</param>
    /// <param name="refreshToken">Testigo de renovación, si lo hubo.</param>
    /// <returns>La tarea que representa el guardado.</returns>
    public async ValueTask SetOAuthTokensAsync(string accessToken, string? refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);

        if (refreshToken is not null)
        {
            await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
        }
    }

    /// <summary>
    /// Borra los testigos de OAuth.
    /// </summary>
    /// <returns>La tarea que representa el borrado.</returns>
    public async ValueTask ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }
}
