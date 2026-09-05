namespace Persiltech.Email.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddSmtpEmailSender_RegistersTheSmtpImplementation()
    {
        using var provider = BuildProvider(options =>
        {
            options.Host = "smtp.example.com";
            options.FromAddress = "no-reply@example.com";
        });

        using var scope = provider.CreateScope();

        Assert.IsType<SmtpEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void AddSmtpEmailSender_RegistersTheOptionsValidator()
    {
        using var provider = BuildProvider(options =>
        {
            options.Host = "smtp.example.com";
            options.FromAddress = "no-reply@example.com";
        });

        Assert.IsType<SmtpOptionsValidator>(
            provider.GetRequiredService<IValidateOptions<SmtpOptions>>());
    }

    [Fact]
    public void AddSmtpEmailSender_RejectsInvalidOptions()
    {
        using var provider = BuildProvider(options => options.FromAddress = "no-reply@example.com");

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);

        Assert.Contains("Host es obligatorio.", exception.Failures);
    }

    [Fact]
    public void AddSmtpEmailSender_RespectsAValidatorRegisteredBeforehand()
    {
        var validator = Substitute.For<IValidateOptions<SmtpOptions>>();

        var services = new ServiceCollection();
        services.AddSingleton(validator);
        services.AddSmtpEmailSender(options => options.Host = "smtp.example.com");

        using var provider = services.BuildServiceProvider();

        Assert.Same(validator, provider.GetRequiredService<IValidateOptions<SmtpOptions>>());
    }

    [Fact]
    public void AddSmtpEmailSender_ThrowsWithoutConfigurationDelegate()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddSmtpEmailSender(null!));
    }

    private static ServiceProvider BuildProvider(Action<SmtpOptions> configureOptions)
    {
        var services = new ServiceCollection();

        services.AddSmtpEmailSender(configureOptions);

        return services.BuildServiceProvider();
    }
}
