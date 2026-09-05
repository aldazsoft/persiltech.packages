namespace Persiltech.Membership.Tests;

public class PasswordEmailAndPhoneEndpointsTests
{
    [Fact]
    public void MapPasswordEndpointsMountsTheThreeRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapPasswordEndpoints());

        Assert.Equal(
            ["password/change", "password/forgot", "password/reset"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapEmailEndpointsMountsTheFourRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapEmailEndpoints());

        Assert.Equal(
            ["email/change", "email/change/confirm", "email/confirmation", "email/confirmation/send"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MapPhoneNumberEndpointsMountsTheTwoRoutesOnTheDefaultPattern()
    {
        var endpoints = Map(app => app.MapPhoneNumberEndpoints());

        Assert.Equal(
            ["phone/change", "phone/change/confirm"],
            endpoints.Select(endpoint => endpoint.RoutePattern.RawText).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryGroupHonoursTheSuppliedPattern()
    {
        var endpoints = Map(app =>
        {
            app.MapPasswordEndpoints("account/password");
            app.MapEmailEndpoints("account/email");
            app.MapPhoneNumberEndpoints("account/phone");
        });

        Assert.All(
            endpoints,
            endpoint => Assert.StartsWith("account/", endpoint.RoutePattern.RawText!, StringComparison.Ordinal));
    }

    [Fact]
    public void OnlyTheRoutesThatCannotCarryATokenAreAnonymous()
    {
        var endpoints = Map(app =>
        {
            app.MapPasswordEndpoints();
            app.MapEmailEndpoints();
            app.MapPhoneNumberEndpoints();
        });

        var anonymous = endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .Order(StringComparer.Ordinal);

        Assert.Equal(
            ["email/confirmation", "email/confirmation/send", "password/forgot", "password/reset"],
            anonymous);
    }

    [Fact]
    public void EveryRouteAnswersOnlyToPost()
    {
        var endpoints = Map(app =>
        {
            app.MapPasswordEndpoints();
            app.MapEmailEndpoints();
            app.MapPhoneNumberEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Equal(
                "POST",
                Assert.Single(endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)));
    }

    [Fact]
    public void NoRouteFixesAnAuthorizationPolicy()
    {
        var endpoints = Map(app =>
        {
            app.MapPasswordEndpoints();
            app.MapEmailEndpoints();
            app.MapPhoneNumberEndpoints();
        });

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public void AddMembershipServicesDoesNotRegisterTheOutgoingPorts()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMembershipServices(ConfigureJwtOptions, _ => { });

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IMembershipEmailSender)
                || descriptor.ServiceType == typeof(IMembershipSmsSender));
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
