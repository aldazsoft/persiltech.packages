namespace Persiltech.Membership.Tests;

public class RoleAndUserEndpointsTests
{
    [Fact]
    public void MapRoleEndpointsMountsTheSixRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapRoleEndpoints());

        Assert.Equal(
            ["roles", "roles", "roles/paged", "roles/{id}", "roles/{id}", "roles/{id}"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapRoleEndpointsHonoursTheSuppliedPattern()
    {
        var endpoints = Map(app => app.MapRoleEndpoints("security/roles"));

        Assert.All(
            endpoints,
            endpoint => Assert.StartsWith("security/roles", endpoint.RoutePattern.RawText!, StringComparison.Ordinal));
    }

    [Fact]
    public void MapRoleEndpointsMountsOneRouteForEachHttpMethod()
    {
        var endpoints = Map(app => app.MapRoleEndpoints());

        Assert.Equal(
            ["DELETE", "GET", "GET", "GET", "POST", "PUT"],
            endpoints.SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods).Order());
    }

    [Fact]
    public void MapUserEndpointsMountsTheFiveRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapUserEndpoints());

        Assert.Equal(
            ["users/current", "users/paged", "users/{id}", "users/{id}/roles", "users/{id}/status"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapUserEndpointsHonoursTheSuppliedPattern()
    {
        var endpoints = Map(app => app.MapUserEndpoints("security/users"));

        Assert.All(
            endpoints,
            endpoint => Assert.StartsWith("security/users", endpoint.RoutePattern.RawText!, StringComparison.Ordinal));
    }

    [Fact]
    public void NoAdministrationEndpointAllowsAnonymousAccess()
    {
        var endpoints = Map(app =>
        {
            app.MapRoleEndpoints();
            app.MapUserEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public void NoAdministrationEndpointFixesAnAuthorizationPolicy()
    {
        var endpoints = Map(app =>
        {
            app.MapRoleEndpoints();
            app.MapUserEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public void TheConsumerCanChainItsOwnAuthorizationPolicy()
    {
        var endpoints = Map(app => app.MapRoleEndpoints().MapUserEndpoints());

        Assert.NotEmpty(endpoints);
        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public void EveryAdministrationEndpointIsTaggedAsMembership()
    {
        var endpoints = Map(app =>
        {
            app.MapRoleEndpoints();
            app.MapUserEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Equal(
                "Membership",
                Assert.Single(endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags)));
    }

    [Fact]
    public void NoAdministrationEndpointDeclaresAName()
    {
        var endpoints = Map(app =>
        {
            app.MapRoleEndpoints();
            app.MapUserEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

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
