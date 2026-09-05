namespace Persiltech.Email.Tests;

public class SmtpOptionsValidatorTests
{
    private readonly SmtpOptionsValidator Validator = new();

    [Fact]
    public void Validate_AcceptsTheMinimumConfiguration()
    {
        var result = Validator.Validate(name: null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyHost(string host)
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.Host = host));

        Assert.Contains("Host es obligatorio.", result.Failures!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_RejectsAPortOutOfRange(int port)
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.Port = port));

        Assert.Contains(result.Failures!, failure => failure.StartsWith("Port", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsAnEmptyFromAddress()
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.FromAddress = string.Empty));

        Assert.Contains("FromAddress es obligatoria.", result.Failures!);
    }

    [Theory]
    [InlineData("no-es-un-correo")]
    [InlineData("sin@dominio@doble.com")]
    public void Validate_RejectsAFromAddressThatTheSenderCouldNotParse(string fromAddress)
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.FromAddress = fromAddress));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("FromAddress no es una dirección", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsAFromAddressWithDisplayName()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.FromAddress = "Persiltech <no-reply@example.com>"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsAUserNameWithoutPassword()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.UserName = "no-reply@example.com"));

        Assert.Contains("Password es obligatoria cuando se configura UserName.", result.Failures!);
    }

    [Fact]
    public void Validate_AcceptsCredentialsThatAreComplete()
    {
        var result = Validator.Validate(name: null, CreateOptions(options =>
        {
            options.UserName = "no-reply@example.com";
            options.Password = "Passw0rd!";
        }));

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(601)]
    public void Validate_RejectsATimeoutOutOfRange(int timeoutInSeconds)
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.TimeoutInSeconds = timeoutInSeconds));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("TimeoutInSeconds", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsEveryFailureAtOnce()
    {
        var result = Validator.Validate(name: null, new SmtpOptions
        {
            Host = string.Empty,
            Port = 0,
            FromAddress = string.Empty,
            UserName = "no-reply@example.com",
            TimeoutInSeconds = 0
        });

        Assert.False(result.Succeeded);
        Assert.Equal(5, result.Failures!.Count());
    }

    private static SmtpOptions CreateOptions(Action<SmtpOptions>? configureOptions = null)
    {
        var options = new SmtpOptions
        {
            Host = "smtp.example.com",
            FromAddress = "no-reply@example.com"
        };

        configureOptions?.Invoke(options);

        return options;
    }
}
