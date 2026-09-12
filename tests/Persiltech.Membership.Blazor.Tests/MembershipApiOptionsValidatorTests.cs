namespace Persiltech.Membership.Blazor.Tests;

public class MembershipApiOptionsValidatorTests
{
    private readonly MembershipApiOptionsValidator Validator = new();

    [Fact]
    public void Validate_AcceptsTheDefaultsWithABaseAddress()
    {
        var result = Validator.Validate(name: null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyBaseAddress(string baseAddress)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.BaseAddress = baseAddress));

        Assert.Contains("BaseAddress es obligatoria.", result.Failures!);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("membership.test")]
    public void Validate_RejectsABaseAddressThatIsNotAbsolute(string baseAddress)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.BaseAddress = baseAddress));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("BaseAddress tiene que ser", StringComparison.Ordinal));
    }

    /// <summary>
    /// Las rutas se concatenan a la dirección base, así que una absoluta se comería el camino
    /// de la base y dejaría las peticiones en la raíz del dominio.
    /// </summary>
    [Fact]
    public void Validate_RejectsAPathThatStartsWithASlash()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.LoginPath = "/user/login"));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("LoginPath es relativa", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsAnEmptyPath()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.UsersPath = string.Empty));

        Assert.Contains("UsersPath es obligatoria.", result.Failures!);
    }

    [Fact]
    public void Validate_ReportsEveryFailureAtOnce()
    {
        var result = Validator.Validate(
            name: null,
            CreateOptions(options =>
            {
                options.BaseAddress = string.Empty;
                options.LoginPath = "/user/login";
                options.RolesPath = string.Empty;
            }));

        Assert.Equal(3, result.Failures!.Count());
    }

    private static MembershipApiOptions CreateOptions(Action<MembershipApiOptions>? configure = null)
    {
        var options = new MembershipApiOptions
        {
            BaseAddress = "https://membership.persiltech.test/"
        };

        configure?.Invoke(options);

        return options;
    }
}
