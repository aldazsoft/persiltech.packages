namespace Persiltech.Membership.Tests;

public class ProfileAndTwoFactorEndpointsTests
{
    [Fact]
    public void AddMembershipServicesRegistersTheIdentityTokenProviders()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipServices(ConfigureJwtOptions, _ => { });

        var identity = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<IdentityOptions>>()
            .Value;

        Assert.Contains("Default", identity.Tokens.ProviderMap.Keys);
        Assert.Contains("Email", identity.Tokens.ProviderMap.Keys);
        Assert.Contains("Phone", identity.Tokens.ProviderMap.Keys);
        Assert.Contains(identity.Tokens.AuthenticatorTokenProvider, identity.Tokens.ProviderMap.Keys);
    }

    [Fact]
    public void MapProfileEndpointsMountsBothRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapProfileEndpoints());

        Assert.Equal(["profile", "profile"], endpoints.Select(endpoint => endpoint.RoutePattern.RawText));

        Assert.Equal(
            ["DELETE", "PUT"],
            endpoints.SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapTwoFactorEndpointsMountsTheFourRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapTwoFactorEndpoints());

        Assert.Equal(
            ["twofactor/disable", "twofactor/enable", "twofactor/recovery-codes", "twofactor/setup"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void NeitherGroupIsAnonymous()
    {
        var endpoints = Map(app =>
        {
            app.MapProfileEndpoints();
            app.MapTwoFactorEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public void BothGroupsHonourTheSuppliedPattern()
    {
        var endpoints = Map(app =>
        {
            app.MapProfileEndpoints("account/profile");
            app.MapTwoFactorEndpoints("account/twofactor");
        });

        Assert.All(
            endpoints,
            endpoint => Assert.StartsWith("account/", endpoint.RoutePattern.RawText!, StringComparison.Ordinal));
    }

    [Fact]
    public void TheSecondFactorTravelsInTheLoginRequestAndIsOptional()
    {
        var request = new LoginUserRequest { Email = "a@b.c", Password = "x" };

        Assert.Null(request.TwoFactorCode);
        Assert.True(RequestValidation.TryValidate(request, out _));
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
