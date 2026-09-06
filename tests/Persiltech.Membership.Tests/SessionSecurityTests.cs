namespace Persiltech.Membership.Tests;

public class SessionSecurityTests
{
    [Fact]
    public void TheOAuthServerMountsUserInfoAndEndSession()
    {
        var endpoints = MapOAuth();

        Assert.Contains("/connect/userinfo", endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
        Assert.Contains("/connect/logout", endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
    }

    [Fact]
    public void TheRevocationEndpointIsNotMountedBecauseOpenIddictResolvesItWhole()
    {
        var endpoints = MapOAuth();

        Assert.DoesNotContain("/connect/revoke", endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
    }

    [Fact]
    public void UserInfoRequiresAToken()
    {
        var userInfo = Assert.Single(
            MapOAuth(),
            endpoint => endpoint.RoutePattern.RawText == "/connect/userinfo");

        Assert.Null(userInfo.Metadata.GetMetadata<IAllowAnonymous>());
    }

    [Fact]
    public void EndSessionIsAnonymousBecauseItRunsWhenTheSessionMayBeGone()
    {
        var endSession = Assert.Single(
            MapOAuth(),
            endpoint => endpoint.RoutePattern.RawText == "/connect/logout");

        Assert.NotNull(endSession.Metadata.GetMetadata<IAllowAnonymous>());
    }

    [Fact]
    public void TheOAuthEndpointPathsAreConfigurable()
    {
        var endpoints = MapOAuth(options =>
        {
            options.UserInfoEndpointPath = "/oauth2/me";
            options.EndSessionEndpointPath = "/oauth2/logout";
        });

        Assert.Contains("/oauth2/me", endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
        Assert.Contains("/oauth2/logout", endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
    }

    [Fact]
    public async Task SeedMembershipAdministratorRejectsMissingArguments()
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.SeedMembershipAdministratorAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void TheAdministratorCarriesTheRoleNameItWillCreate()
    {
        var administrator = new MembershipAdministrator(
            "admin@example.com",
            "Passw0rd!",
            "Ada",
            "Lovelace");

        Assert.Equal("Administrator", administrator.RoleName);
    }

    private static List<RouteEndpoint> MapOAuth(Action<MembershipOAuthOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipServices(ConfigureJwtOptions, _ => { });
        builder.Services.AddMembershipOAuthServer(
            _ => { },
            options =>
            {
                options.UseDevelopmentCertificates = true;
                configure?.Invoke(options);
            });

        var app = builder.Build();

        app.MapMembershipOAuthEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static void ConfigureJwtOptions(JwtOptions options)
    {
        options.SecurityKey = "una-clave-de-firma-de-32-caracteres";
        options.ValidIssuer = "https://membership.persiltech.test";
        options.ValidAudience = "persiltech-sample";
        options.ExpireInMinutes = 30;
    }
}
