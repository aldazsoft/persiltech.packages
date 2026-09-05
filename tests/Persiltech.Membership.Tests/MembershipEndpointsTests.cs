namespace Persiltech.Membership.Tests;

public class MembershipEndpointsTests
{
    [Fact]
    public void MapMembershipEndpointsMountsBothEndpointsOnTheDefaultPatterns()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints());

        Assert.Equal(
            ["user/register", "user/login"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
    }

    [Fact]
    public void MapMembershipEndpointsHonoursTheSuppliedPatterns()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints("auth/signup", "auth/signin"));

        Assert.Equal(
            ["auth/signup", "auth/signin"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText));
    }

    [Fact]
    public void BothEndpointsAnswerOnlyToPost()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints());

        Assert.All(
            endpoints,
            endpoint => Assert.Equal(
                "POST",
                Assert.Single(endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)));
    }

    [Fact]
    public void BothEndpointsAllowAnonymousAccess()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints());

        Assert.All(
            endpoints,
            endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public void BothEndpointsShareTheMembershipTag()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints());

        Assert.All(
            endpoints,
            endpoint => Assert.Equal(
                "Membership",
                Assert.Single(endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags)));
    }

    [Fact]
    public void NeitherEndpointIsNamed()
    {
        var endpoints = Map(app => app.MapMembershipEndpoints());

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

    [Fact]
    public void TheRegistrationEndpointDescribesItsResponses()
    {
        var endpoint = Assert.Single(Map(app => app.MapUserRegistrationEndpoint("user/register")));

        Assert.Equal(
            "Registrar una cuenta",
            endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal([201, 400], ResponseStatusCodes(endpoint));
    }

    [Fact]
    public void TheLoginEndpointDescribesItsResponses()
    {
        var endpoint = Assert.Single(Map(app => app.MapUserLoginEndpoint("user/login")));

        Assert.Equal(
            "Autenticar a un usuario",
            endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary);
        Assert.Equal([200, 400], ResponseStatusCodes(endpoint));
        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.Type == typeof(LoginUserResponse));
    }

    private static IEnumerable<int> ResponseStatusCodes(RouteEndpoint endpoint) =>
        endpoint.Metadata
            .OfType<IProducesResponseTypeMetadata>()
            .Select(metadata => metadata.StatusCode)
            .Order();

    private static List<RouteEndpoint> Map(Action<IEndpointRouteBuilder> map)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipServices(ConfigureJwtOptions, _ => { });

        var app = builder.Build();

        map(app);

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
