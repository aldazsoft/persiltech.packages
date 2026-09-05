namespace Persiltech.Membership.Email.Tests;

public sealed class MembershipEmailOptionsValidatorTests : IDisposable
{
    private readonly string TemplatesDirectory =
        Path.Combine(Path.GetTempPath(), $"persiltech-membership-email-options-{Guid.NewGuid():N}");

    private readonly MembershipEmailOptionsValidator Validator = new();

    public void Dispose()
    {
        if (Directory.Exists(TemplatesDirectory))
        {
            Directory.Delete(TemplatesDirectory, recursive: true);
        }
    }

    [Fact]
    public void Validate_AcceptsTheMinimumConfiguration()
    {
        var result = Validator.Validate(name: null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsAnEmptyBrandName()
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.BrandName = "  "));

        Assert.Contains("BrandName es obligatorio.", result.Failures!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("app.example.com")]
    [InlineData("/confirm-email")]
    [InlineData("ftp://app.example.com")]
    public void Validate_RejectsAClientBaseUrlThatIsNotAnAbsoluteHttpUrl(string clientBaseUrl)
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.ClientBaseUrl = clientBaseUrl));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("ClientBaseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsARelativeLogoUrl()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.LogoUrl = "/assets/logo.png"));

        Assert.Contains(result.Failures!, failure => failure.StartsWith("LogoUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsTheAbsenceOfLogoUrl()
    {
        var result = Validator.Validate(name: null, CreateOptions(options => options.LogoUrl = null));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsAnEmptyPath()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.PasswordResetPath = string.Empty));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("PasswordResetPath", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("0d6efd")]
    [InlineData("#0d6ef")]
    [InlineData("#azul")]
    [InlineData("")]
    public void Validate_RejectsAColorThatIsNotHexadecimal(string primaryColor)
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.PrimaryColor = primaryColor));

        Assert.Contains(result.Failures!, failure => failure.StartsWith("PrimaryColor", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("#fff")]
    [InlineData("#0d6efd")]
    [InlineData("#0D6EFD")]
    public void Validate_AcceptsTheHexadecimalColors(string primaryColor)
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.PrimaryColor = primaryColor));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsASupportEmailThatIsNotAnAddress()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.SupportEmail = "soporte"));

        Assert.Contains(result.Failures!, failure => failure.StartsWith("SupportEmail", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsATemplatesDirectoryThatDoesNotExist()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.TemplatesDirectory = TemplatesDirectory));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("TemplatesDirectory", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsATemplatesDirectoryThatExists()
    {
        Directory.CreateDirectory(TemplatesDirectory);

        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.TemplatesDirectory = TemplatesDirectory));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ReportsEveryFailureAtOnce()
    {
        var result = Validator.Validate(name: null, new MembershipEmailOptions
        {
            BrandName = string.Empty,
            ClientBaseUrl = string.Empty,
            PrimaryColor = "azul",
            SupportEmail = "soporte"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(4, result.Failures!.Count());
    }

    private static MembershipEmailOptions CreateOptions(Action<MembershipEmailOptions>? configureOptions = null)
    {
        var options = new MembershipEmailOptions
        {
            BrandName = "Persiltech",
            ClientBaseUrl = "https://app.example.com"
        };

        configureOptions?.Invoke(options);

        return options;
    }
}
