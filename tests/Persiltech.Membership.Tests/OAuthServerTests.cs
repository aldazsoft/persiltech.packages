namespace Persiltech.Membership.Tests;

public class OAuthServerTests
{
    [Fact]
    public void MapMembershipOAuthEndpointsMountsTheConfiguredRoutes()
    {
        var endpoints = Map();

        Assert.Equal(
            ["/connect/authorize", "/connect/logout", "/connect/token", "/connect/userinfo"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapMembershipOAuthEndpointsHonoursTheConfiguredPaths()
    {
        var endpoints = Map(options =>
        {
            options.AuthorizationEndpointPath = "/oauth2/authorize";
            options.TokenEndpointPath = "/oauth2/token";
        });

        Assert.Equal(
            ["/connect/logout", "/connect/userinfo", "/oauth2/authorize", "/oauth2/token"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheAuthorizationEndpointAnswersToGetAndPost()
    {
        var authorize = Assert.Single(
            Map(),
            endpoint => endpoint.RoutePattern.RawText == "/connect/authorize");

        Assert.Equal(
            ["GET", "POST"],
            authorize.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheTokenEndpointAnswersOnlyToPost()
    {
        var token = Assert.Single(
            Map(),
            endpoint => endpoint.RoutePattern.RawText == "/connect/token");

        Assert.Equal(
            "POST",
            Assert.Single(token.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods));
    }

    [Fact]
    public void EveryEndpointIsAnonymousExceptUserInfo()
    {
        var anonymous = Map()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .Order(StringComparer.Ordinal);

        Assert.Equal(["/connect/authorize", "/connect/logout", "/connect/token"], anonymous);
    }

    [Fact]
    public void AddMembershipOAuthServerRegistersItsOwnDbContext()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipOAuthServer(_ => { }, ConfigureOAuthOptions);

        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(MembershipOAuthDbContext));
    }

    [Fact]
    public void AddMembershipOAuthServerRejectsMissingArguments()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddMembershipOAuthServer(null!, ConfigureOAuthOptions));

        Assert.Throws<ArgumentNullException>(
            () => services.AddMembershipOAuthServer(_ => { }, null!));
    }

    private static List<RouteEndpoint> Map(Action<MembershipOAuthOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipServices(ConfigureJwtOptions, _ => { });
        builder.Services.AddMembershipOAuthServer(
            _ => { },
            options =>
            {
                ConfigureOAuthOptions(options);
                configure?.Invoke(options);
            });

        var app = builder.Build();

        app.MapMembershipOAuthEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static void ConfigureOAuthOptions(MembershipOAuthOptions options) =>
        options.UseDevelopmentCertificates = true;

    private static void ConfigureJwtOptions(JwtOptions options)
    {
        options.SecurityKey = "una-clave-de-firma-de-32-caracteres";
        options.ValidIssuer = "https://membership.persiltech.test";
        options.ValidAudience = "persiltech-sample";
        options.ExpireInMinutes = 30;
    }
}
