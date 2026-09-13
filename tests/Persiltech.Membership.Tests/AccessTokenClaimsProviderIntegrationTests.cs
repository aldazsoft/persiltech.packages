namespace Persiltech.Membership.Tests;

/// <summary>
/// Las reclamaciones que aporta el consumidor tienen que llegar al token que devuelve el login.
/// </summary>
/// <remarks>
/// Lo que se prueba aquí no es la fábrica —eso ya está cubierto— sino el cableado: que el
/// registro del consumidor lo vea el emisor y que el token que sale por HTTP las traiga. Es el
/// tramo donde un ámbito mal elegido o un servicio sin registrar no se nota hasta producción.
/// </remarks>
public class AccessTokenClaimsProviderIntegrationTests
{
    [Fact]
    public async Task TheLoginTokenCarriesTheClaimsThatTheApplicationContributes()
    {
        await using var application = await MembershipApplication.StartAsync(
            configureServices: services =>
                services.AddScoped<IAccessTokenClaimsProvider, TenantClaimsProvider>());

        var accessToken = await application.RegisterAndLoginAsync();

        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

        Assert.Equal("42", token.GetClaim("CustomerId").Value);
        Assert.Equal("dev", token.GetClaim("CustomerCode").Value);
    }

    /// <summary>
    /// El emisor tiene ámbito para poder depender de servicios con ámbito. Si volviera a ser
    /// único, resolverlo fallaría aquí en lugar de capturar el servicio en silencio.
    /// </summary>
    [Fact]
    public async Task TheTokenFactoryCanDependOnScopedServices()
    {
        await using var application = await MembershipApplication.StartAsync(
            configureServices: services =>
            {
                services.AddScoped<ScopedDependency>();
                services.AddScoped<IAccessTokenClaimsProvider, ScopedClaimsProvider>();
            });

        var accessToken = await application.RegisterAndLoginAsync();

        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

        Assert.Equal("con-ambito", token.GetClaim("Scoped").Value);
    }

    [Fact]
    public async Task TheRefreshedTokenAlsoCarriesThem()
    {
        await using var application = await MembershipApplication.StartAsync(
            configureServices: services =>
                services.AddScoped<IAccessTokenClaimsProvider, TenantClaimsProvider>());

        await application.RegisterAndLoginAsync();

        var login = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "Passw0rd!" },
            TestContext.Current.CancellationToken);

        var issued = await login.Content.ReadFromJsonAsync<LoginUserResponse>(
            TestContext.Current.CancellationToken);

        var refreshed = await application.Client.PostAsJsonAsync(
            "user/refresh",
            new { refreshToken = issued!.RefreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);

        var rotated = await refreshed.Content.ReadFromJsonAsync<LoginUserResponse>(
            TestContext.Current.CancellationToken);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(rotated!.AccessToken);

        Assert.Equal("42", token.GetClaim("CustomerId").Value);
    }

    [Fact]
    public async Task TheLoginFailsLoudlyWhenAProviderTriesToGrantItselfARole()
    {
        await using var application = await MembershipApplication.StartAsync(
            configureServices: services =>
                services.AddScoped<IAccessTokenClaimsProvider, RoleUsurpingClaimsProvider>());

        var registered = await application.Client.PostAsJsonAsync(
            "user/register",
            new { email = "juan.perez@example.com", password = "Passw0rd!", firstName = "Juan", lastName = "Pérez" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            application.Client.PostAsJsonAsync(
                "user/login",
                new { email = "juan.perez@example.com", password = "Passw0rd!" },
                TestContext.Current.CancellationToken));
    }

    private sealed class TenantClaimsProvider : IAccessTokenClaimsProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["CustomerId"] = "42",
                    ["CustomerCode"] = "dev"
                });
    }

    private sealed class ScopedDependency
    {
        public string Value => "con-ambito";
    }

    private sealed class ScopedClaimsProvider(ScopedDependency dependency) : IAccessTokenClaimsProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Scoped"] = dependency.Value
                });
    }

    private sealed class RoleUsurpingClaimsProvider : IAccessTokenClaimsProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [ClaimTypes.Role] = "Administrators"
                });
    }
}
