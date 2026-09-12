namespace Persiltech.Membership.Tests;

public class MembershipOAuthOptionsValidatorTests
{
    private readonly MembershipOAuthOptionsValidator Validator = new();

    [Fact]
    public void Validate_AcceptsTheDefaults()
    {
        var result = Validator.Validate(name: null, new MembershipOAuthOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyPath(string path)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.TokenEndpointPath = path));

        Assert.Contains("TokenEndpointPath es obligatoria.", result.Failures!);
    }

    [Fact]
    public void Validate_RejectsAPathThatDoesNotStartWithASlash()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.TokenEndpointPath = "connect/token"));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("TokenEndpointPath tiene que empezar", StringComparison.Ordinal));
    }

    /// <summary>
    /// Es lo que una anotación no alcanza: cada ruta por separado es válida, y juntas dejan
    /// dos endpoints en el mismo sitio.
    /// </summary>
    [Fact]
    public void Validate_RejectsTwoEndpointsOnTheSamePath()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.UserInfoEndpointPath = options.TokenEndpointPath));

        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("está repetida", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ComparesPathsIgnoringCase()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.UserInfoEndpointPath = "/CONNECT/TOKEN"));

        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("está repetida", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsAnEmptyInteractiveScheme()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options => options.InteractiveAuthenticationScheme = string.Empty));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("InteractiveAuthenticationScheme", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsAnAccessTokenLifetimeThatIsNotPositive(int minutes)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.AccessTokenLifetimeInMinutes = minutes));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("AccessTokenLifetimeInMinutes", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsARefreshTokenLifetimeThatIsNotPositive(int days)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.RefreshTokenLifetimeInDays = days));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("RefreshTokenLifetimeInDays", StringComparison.Ordinal));
    }

    private static MembershipOAuthOptions CreateOptions(Action<MembershipOAuthOptions> configure)
    {
        var options = new MembershipOAuthOptions();

        configure(options);

        return options;
    }
}
