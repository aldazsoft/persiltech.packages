namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Verifica cómo se deriva el estado de autenticación del token, y la renovación.
/// </summary>
public sealed class MembershipAuthenticationStateProviderTests
{
    [Fact]
    public async Task SinTestigosLaSesionEsAnonima()
    {
        var store = new FakeTokenStore(null);
        var api = Substitute.For<IMembershipApiClient>();

        var state = await new MembershipAuthenticationStateProvider(store, api)
            .GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task UnTokenVigenteRindeSusReclamaciones()
    {
        var token = TokenWith(TimeSpan.FromMinutes(30), ("name", "ada@example.com"), ("role", "Administrador"));
        var store = new FakeTokenStore(new MembershipTokens(token, "refresh"));

        var state = await new MembershipAuthenticationStateProvider(store, Substitute.For<IMembershipApiClient>())
            .GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("ada@example.com", state.User.Identity?.Name);
        Assert.True(state.User.IsInRole("Administrador"));
    }

    [Fact]
    public async Task UnTokenIlegibleCierraLaSesion()
    {
        var store = new FakeTokenStore(new MembershipTokens("esto-no-es-un-jwt", "refresh"));

        var state = await new MembershipAuthenticationStateProvider(store, Substitute.For<IMembershipApiClient>())
            .GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(store.Current);
    }

    [Fact]
    public async Task UnTokenCaducadoSeRenuevaEnVezDeCerrarLaSesion()
    {
        var expired = TokenWith(TimeSpan.FromMinutes(-5), ("name", "ada@example.com"));
        var fresh = TokenWith(TimeSpan.FromMinutes(30), ("name", "ada@example.com"));

        var store = new FakeTokenStore(new MembershipTokens(expired, "refresh-1"));
        var api = Substitute.For<IMembershipApiClient>();

        api.RefreshAsync("refresh-1").Returns(
            ApiResult<MembershipTokens>.Success(new MembershipTokens(fresh, "refresh-2"), HttpStatusCode.OK));

        var state = await new MembershipAuthenticationStateProvider(store, api).GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("refresh-2", store.Current?.RefreshToken);
    }

    [Fact]
    public async Task SiLaRenovacionFallaLaSesionSeCierra()
    {
        var expired = TokenWith(TimeSpan.FromMinutes(-5), ("name", "ada@example.com"));
        var store = new FakeTokenStore(new MembershipTokens(expired, "refresh-1"));
        var api = Substitute.For<IMembershipApiClient>();

        api.RefreshAsync("refresh-1").Returns(
            ApiResult<MembershipTokens>.Failure(HttpStatusCode.Unauthorized));

        var state = await new MembershipAuthenticationStateProvider(store, api).GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(store.Current);
    }

    [Fact]
    public async Task CerrarSesionAvisaALaApiAntesDeBorrar()
    {
        var token = TokenWith(TimeSpan.FromMinutes(30), ("name", "ada@example.com"));
        var store = new FakeTokenStore(new MembershipTokens(token, "refresh-1"));
        var api = Substitute.For<IMembershipApiClient>();

        api.LogoutAsync("refresh-1").Returns(ApiResult<Unit>.Success(null, HttpStatusCode.NoContent));

        await new MembershipAuthenticationStateProvider(store, api).SignOutAsync();

        // Borrarlo solo del navegador lo dejaría vivo para quien lo hubiera copiado.
        await api.Received(1).LogoutAsync("refresh-1");
        Assert.Null(store.Current);
    }

    [Fact]
    public async Task IniciarSesionGuardaElParYPasaAAutenticado()
    {
        var token = TokenWith(TimeSpan.FromMinutes(30), ("name", "ada@example.com"));
        var store = new FakeTokenStore(null);
        var provider = new MembershipAuthenticationStateProvider(store, Substitute.For<IMembershipApiClient>());

        await provider.SignInAsync(new MembershipTokens(token, "refresh-1"));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("refresh-1", store.Current?.RefreshToken);
    }

    // Un JWT sin firmar: el proveedor lee el cuerpo, no lo valida, así que la firma no
    // interviene. Es justo lo que documenta el paquete.
    private static string TokenWith(TimeSpan lifetime, params (string Key, string Value)[] claims)
    {
        var payload = claims.ToDictionary(claim => claim.Key, claim => (object)claim.Value);

        payload["exp"] = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();

        return $"{Encode("{\"alg\":\"none\"}")}.{Encode(JsonSerializer.Serialize(payload))}.firma";
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed class FakeTokenStore(MembershipTokens? initial) : IMembershipTokenStore
    {
        internal MembershipTokens? Current { get; private set; } = initial;

        public ValueTask<MembershipTokens?> GetAsync() => ValueTask.FromResult(Current);

        public ValueTask SetAsync(MembershipTokens tokens)
        {
            Current = tokens;

            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync()
        {
            Current = null;

            return ValueTask.CompletedTask;
        }
    }
}
