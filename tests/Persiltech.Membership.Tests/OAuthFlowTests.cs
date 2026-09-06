namespace Persiltech.Membership.Tests;

/// <summary>
/// Ejerce los flujos del servidor de autorización contra un servidor en marcha.
/// </summary>
/// <remarks>
/// Complementa a <see cref="OAuthServerTests"/>, que comprueba el cableado —rutas, verbos
/// y autorización— sin llegar a emitir un testigo. Aquí se comprueba lo que decide si el
/// servidor es seguro: qué concesiones se admiten, cuáles se rechazan y qué viaja dentro
/// del testigo emitido.
/// </remarks>
public class OAuthFlowTests
{
    [Fact]
    public async Task TheClientCredentialsGrantIssuesAnAccessToken()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = OAuthApplication.ConfidentialClientId,
            ["client_secret"] = OAuthApplication.ConfidentialClientSecret,
            ["scope"] = Scopes.Email
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("access_token").GetString()));
        Assert.Equal("Bearer", body.GetProperty("token_type").GetString());
    }

    // El subject de un testigo de máquina es el propio cliente: si saliera con el de una
    // persona, un servicio quedaría suplantando a un usuario.
    [Fact]
    public async Task TheMachineTokenCarriesTheClientAsItsSubject()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = OAuthApplication.ConfidentialClientId,
            ["client_secret"] = OAuthApplication.ConfidentialClientSecret,
            ["scope"] = Scopes.Email
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("access_token").GetString());

        Assert.Equal(
            OAuthApplication.ConfidentialClientId,
            token.Claims.Single(claim => claim.Type == Claims.Subject).Value);
    }

    [Fact]
    public async Task AWrongClientSecretIsRejected()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = OAuthApplication.ConfidentialClientId,
            ["client_secret"] = "el-secreto-equivocado",
            ["scope"] = Scopes.Email
        });

        // El RFC 6749 §5.2 pide 401 para invalid_client, no 400.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(Errors.InvalidClient, body.GetProperty("error").GetString());
    }

    // Un cliente público no guarda secreto, así que no puede pedir un testigo de máquina:
    // cualquiera que lea su identificador en el navegador lo obtendría.
    [Fact]
    public async Task APublicClientCannotUseTheClientCredentialsGrant()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = OAuthApplication.PublicClientId,
            ["scope"] = Scopes.Email
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnUnknownClientIsRejected()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "cliente-que-no-existe",
            ["client_secret"] = "cualquiera",
            ["scope"] = Scopes.Email
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(Errors.InvalidClient, body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AnUnsupportedGrantTypeIsRejected()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = OAuthApplication.ConfidentialClientId,
            ["client_secret"] = OAuthApplication.ConfidentialClientSecret,
            ["username"] = "juan.perez@example.com",
            ["password"] = "Passw0rd!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(Errors.UnsupportedGrantType, body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TheUserInfoEndpointRejectsACallWithoutAToken()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.Client.GetAsync("/connect/userinfo", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheUserInfoEndpointRejectsAnInventedToken()
    {
        await using var server = await OAuthApplication.StartAsync();

        var client = server.Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "un-token-inventado");

        var response = await client.GetAsync("/connect/userinfo", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // El endpoint de autorización no puede emitir nada a un visitante sin sesión: tiene que
    // mandarlo a autenticarse. Que respondiera otra cosa sería emitir un código sin saber
    // quién lo pide.
    [Fact]
    public async Task TheAuthorizationEndpointChallengesAnAnonymousVisitor()
    {
        await using var server = await OAuthApplication.StartAsync();

        var response = await server.Client.GetAsync(
            AuthorizeUrl(CodeChallenge),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("code=", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAuthenticatedVisitorGetsAnAuthorizationCode()
    {
        await using var server = await OAuthApplication.StartAsync();

        await server.RegisterAsync();

        var client = await server.SignInAsync();

        var response = await client.GetAsync(AuthorizeUrl(CodeChallenge), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location!.ToString();

        Assert.StartsWith(OAuthApplication.RedirectUri, location, StringComparison.Ordinal);
        Assert.Contains("code=", location, StringComparison.Ordinal);
    }

    // Es la razón de ser de PKCE: sin el verificador, quien intercepte el código no puede
    // canjearlo. El servidor lo exige con RequireProofKeyForCodeExchange, y lo rechaza de
    // plano en lugar de redirigir con un error, que deja menos margen a un cliente
    // descuidado que ignore el parámetro de error de la vuelta.
    [Fact]
    public async Task TheAuthorizationEndpointRefusesARequestWithoutPkce()
    {
        await using var server = await OAuthApplication.StartAsync();

        await server.RegisterAsync();

        var client = await server.SignInAsync();

        var response = await client.GetAsync(
            $"/connect/authorize?client_id={OAuthApplication.PublicClientId}" +
            $"&response_type=code&redirect_uri={Uri.EscapeDataString(OAuthApplication.RedirectUri)}" +
            $"&scope={Uri.EscapeDataString($"{Scopes.OpenId} {Scopes.Email}")}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // La URI de vuelta se compara de forma exacta. Es lo que impide que un tercero que
    // conozca el identificador del cliente se lleve el código a su propio dominio.
    [Fact]
    public async Task AnUnregisteredRedirectUriIsRejected()
    {
        await using var server = await OAuthApplication.StartAsync();

        await server.RegisterAsync();

        var client = await server.SignInAsync();

        var response = await client.GetAsync(
            $"/connect/authorize?client_id={OAuthApplication.PublicClientId}" +
            $"&response_type=code&redirect_uri={Uri.EscapeDataString("https://atacante.test/callback")}" +
            $"&scope={Uri.EscapeDataString(Scopes.OpenId)}" +
            $"&code_challenge={CodeChallenge}&code_challenge_method=S256",
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
    }

    private const string CodeVerifier = "un-verificador-de-codigo-suficientemente-largo-para-pkce";

    private static string CodeChallenge =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(CodeVerifier)));

    private static string AuthorizeUrl(string challenge) =>
        $"/connect/authorize?client_id={OAuthApplication.PublicClientId}" +
        $"&response_type=code&redirect_uri={Uri.EscapeDataString(OAuthApplication.RedirectUri)}" +
        $"&scope={Uri.EscapeDataString($"{Scopes.OpenId} {Scopes.Email}")}" +
        $"&code_challenge={challenge}&code_challenge_method=S256";
}
