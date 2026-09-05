namespace Persiltech.Membership.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMembershipServicesReturnsTheSameCollection()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddMembershipServices(ConfigureJwtOptions, _ => { }));
    }

    [Fact]
    public void AddMembershipServicesRejectsAMissingCollection()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddMembershipServices(ConfigureJwtOptions, _ => { }));
    }

    [Fact]
    public void AddMembershipServicesRejectsAMissingJwtDelegate()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddMembershipServices(null!, _ => { }));
    }

    [Fact]
    public void AddMembershipServicesRejectsAMissingDbContextDelegate()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddMembershipServices(ConfigureJwtOptions, null!));
    }

    [Fact]
    public void AddMembershipServicesRegistersTheDataContextAndIdentity()
    {
        var services = Register();

        Assert.Contains(services, service => service.ServiceType == typeof(MembershipDbContext));
        Assert.Contains(services, service => service.ServiceType == typeof(UserManager<ApplicationUser>));
    }

    [Fact]
    public void AddMembershipServicesLeavesTheAuthenticationSchemeToTheConsumer()
    {
        var services = Register();

        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IAuthenticationSchemeProvider));
    }

    [Fact]
    public void AddMembershipServicesRegistersTheTokenFactoryAsSingleton()
    {
        var descriptor = Assert.Single(
            Register(),
            service => service.ServiceType == typeof(IAccessTokenFactory));

        Assert.Equal(typeof(JwtAccessTokenFactory), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMembershipServicesResolvesTheConfiguredOptions()
    {
        var options = Register().BuildServiceProvider().GetRequiredService<IOptions<JwtOptions>>();

        Assert.Equal(30, options.Value.ExpireInMinutes);
        Assert.Equal("persiltech-sample", options.Value.ValidAudience);
    }

    [Fact]
    public void AddMembershipServicesRejectsASigningKeyShorterThan32Characters()
    {
        var services = new ServiceCollection()
            .AddMembershipServices(jwt => jwt.SecurityKey = "demasiado-corta", _ => { });

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<JwtOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void AddMembershipServicesValidatesTheOptionsOnStart()
    {
        Assert.Contains(Register(), service => service.ServiceType == typeof(IStartupValidator));
    }

    private static IServiceCollection Register() =>
        new ServiceCollection().AddMembershipServices(ConfigureJwtOptions, _ => { });

    private static void ConfigureJwtOptions(JwtOptions options)
    {
        options.SecurityKey = "una-clave-de-firma-de-32-caracteres";
        options.ValidIssuer = "https://membership.persiltech.test";
        options.ValidAudience = "persiltech-sample";
        options.ExpireInMinutes = 30;
    }
}
