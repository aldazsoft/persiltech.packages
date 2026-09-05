namespace Persiltech.Membership.Email.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMembershipEmail_RegistersTheTemplatedAdapter()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<TemplatedMembershipEmailSender>(
            scope.ServiceProvider.GetRequiredService<IMembershipEmailSender>());
    }

    [Fact]
    public void AddMembershipEmail_RegistersTheEmbeddedRenderer()
    {
        using var provider = BuildProvider();

        Assert.IsType<EmbeddedTemplateRenderer>(provider.GetRequiredService<IEmailTemplateRenderer>());
    }

    [Fact]
    public void AddMembershipEmail_RespectsARendererRegisteredBeforehand()
    {
        var renderer = Substitute.For<IEmailTemplateRenderer>();

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IEmailSender>());
        services.AddSingleton(renderer);
        services.AddMembershipEmail(ConfigureValidOptions);

        using var provider = services.BuildServiceProvider();

        Assert.Same(renderer, provider.GetRequiredService<IEmailTemplateRenderer>());
    }

    [Fact]
    public void AddMembershipEmail_RegistersTheOptionsValidator()
    {
        using var provider = BuildProvider();

        Assert.IsType<MembershipEmailOptionsValidator>(
            provider.GetRequiredService<IValidateOptions<MembershipEmailOptions>>());
    }

    [Fact]
    public void AddMembershipEmail_RejectsInvalidOptions()
    {
        var services = new ServiceCollection();
        services.AddMembershipEmail(options => options.BrandName = "Persiltech");

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MembershipEmailOptions>>().Value);

        Assert.Contains("ClientBaseUrl es obligatoria.", exception.Failures);
    }

    [Fact]
    public void AddMembershipEmail_RespectsAValidatorRegisteredBeforehand()
    {
        var validator = Substitute.For<IValidateOptions<MembershipEmailOptions>>();

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IEmailSender>());
        services.AddSingleton(validator);
        services.AddMembershipEmail(ConfigureValidOptions);

        using var provider = services.BuildServiceProvider();

        Assert.Same(validator, provider.GetRequiredService<IValidateOptions<MembershipEmailOptions>>());
    }

    [Fact]
    public void AddMembershipEmail_ThrowsWithoutConfigurationDelegate()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddMembershipEmail(null!));
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<IEmailSender>());
        services.AddMembershipEmail(ConfigureValidOptions);

        return services.BuildServiceProvider();
    }

    private static void ConfigureValidOptions(MembershipEmailOptions options)
    {
        options.BrandName = "Persiltech";
        options.ClientBaseUrl = "https://app.example.com";
    }
}
