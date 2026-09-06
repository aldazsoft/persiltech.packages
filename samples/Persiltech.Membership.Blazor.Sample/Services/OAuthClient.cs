namespace Persiltech.Membership.Blazor.Sample.Services;

/// <summary>
/// El lado cliente del flujo Authorization Code con PKCE contra Persiltech.Membership.OAuth.
/// </summary>
/// <remarks>
/// Esta aplicación es un cliente <em>público</em>: vive en el navegador y no puede guardar un
/// secreto, porque cualquiera puede leer lo que descarga. PKCE es lo que ocupa el lugar del
/// secreto: se envía el resumen de un valor al pedir el código y el valor entero al canjearlo,
/// de modo que quien intercepte el código no pueda usarlo sin el original.
/// </remarks>
public sealed class OAuthClient(HttpClient http, IJSRuntime js, NavigationManager navigation)
{
    private const string VerifierKey = "persiltech.membership.codeVerifier";

    public const string ClientId = "persiltech-spa";

    /// <summary>
    /// Genera el par de PKCE, lo guarda y lleva el navegador al endpoint de autorización.
    /// </summary>
    /// <param name="authorityBaseAddress">Origen del servidor de autorización.</param>
    /// <returns>La tarea que representa el inicio del flujo.</returns>
    public async Task StartAsync(string authorityBaseAddress)
    {
        var verifier = CreateVerifier();

        // El verificador tiene que sobrevivir a la vuelta del navegador, que recarga la
        // aplicación entera: por eso se guarda antes de salir.
        await js.InvokeVoidAsync("sessionStorage.setItem", VerifierKey, verifier);

        var redirectUri = $"{navigation.BaseUri.TrimEnd('/')}/oauth/callback";

        var url =
            $"{authorityBaseAddress.TrimEnd('/')}/connect/authorize" +
            $"?client_id={ClientId}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&scope={Uri.EscapeDataString("openid email profile roles offline_access")}" +
            $"&code_challenge={CreateChallenge(verifier)}" +
            "&code_challenge_method=S256";

        navigation.NavigateTo(url, forceLoad: true);
    }

    /// <summary>
    /// Canjea el código de la vuelta por los testigos.
    /// </summary>
    /// <param name="code">Código de autorización recibido.</param>
    /// <returns>Los testigos emitidos, o el error del servidor.</returns>
    public async Task<ApiResult<OAuthTokens>> ExchangeAsync(string code)
    {
        var verifier = await js.InvokeAsync<string?>("sessionStorage.getItem", VerifierKey);

        if (string.IsNullOrEmpty(verifier))
        {
            return ApiResult<OAuthTokens>.Failure(
                "No se encontró el verificador de PKCE. Vuelve a iniciar el flujo.",
                HttpStatusCode.BadRequest);
        }

        await js.InvokeVoidAsync("sessionStorage.removeItem", VerifierKey);

        var redirectUri = $"{navigation.BaseUri.TrimEnd('/')}/oauth/callback";

        return await PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = verifier
        });
    }

    /// <summary>
    /// Renueva la sesión con el testigo de renovación.
    /// </summary>
    /// <param name="refreshToken">Testigo de renovación.</param>
    /// <returns>Los testigos nuevos, o el error del servidor.</returns>
    public Task<ApiResult<OAuthTokens>> RefreshAsync(string refreshToken) =>
        PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshToken
        });

    /// <summary>
    /// Lee las reclamaciones del usuario desde el endpoint de información.
    /// </summary>
    /// <param name="accessToken">Token de acceso emitido por el servidor.</param>
    /// <returns>El documento de reclamaciones tal cual lo devuelve el servidor.</returns>
    public async Task<ApiResult<Dictionary<string, JsonElement>>> GetUserInfoAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<Dictionary<string, JsonElement>>.Failure(
                    $"El servidor respondió {(int)response.StatusCode}.",
                    response.StatusCode);
            }

            return ApiResult<Dictionary<string, JsonElement>>.Success(
                await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(),
                response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<Dictionary<string, JsonElement>>.Failure(
                exception.Message,
                HttpStatusCode.ServiceUnavailable);
        }
    }

    private async Task<ApiResult<OAuthTokens>> PostAsync(Dictionary<string, string> form)
    {
        try
        {
            var response = await http.PostAsync("connect/token", new FormUrlEncodedContent(form));
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

            if (!response.IsSuccessStatusCode)
            {
                var error = body is not null && body.TryGetValue("error", out var value)
                    ? value.ToString()
                    : $"{(int)response.StatusCode}";

                var description = body is not null && body.TryGetValue("error_description", out var detail)
                    ? detail.ToString()
                    : string.Empty;

                return ApiResult<OAuthTokens>.Failure($"{error}: {description}", response.StatusCode);
            }

            return ApiResult<OAuthTokens>.Success(
                new OAuthTokens(
                    Read(body, "access_token"),
                    Read(body, "id_token"),
                    Read(body, "refresh_token")),
                response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<OAuthTokens>.Failure(exception.Message, HttpStatusCode.ServiceUnavailable);
        }
    }

    private static string? Read(Dictionary<string, JsonElement>? body, string key) =>
        body is not null && body.TryGetValue(key, out var value) ? value.ToString() : null;

    private static string CreateVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Base64Url(bytes);
    }

    private static string CreateChallenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Testigos que devuelve el endpoint de testigos.
/// </summary>
/// <param name="AccessToken">Token de acceso.</param>
/// <param name="IdToken">Token de identidad de OpenID Connect.</param>
/// <param name="RefreshToken">Testigo de renovación, si se pidió offline_access.</param>
public sealed record OAuthTokens(string? AccessToken, string? IdToken, string? RefreshToken);
