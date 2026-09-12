namespace Persiltech.Membership.Tests;

public class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator Validator = new();

    [Fact]
    public void Validate_AcceptsTheMinimumConfiguration()
    {
        var result = Validator.Validate(name: null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptySecurityKey(string securityKey)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.SecurityKey = securityKey));

        Assert.Contains("SecurityKey es obligatoria.", result.Failures!);
    }

    [Fact]
    public void Validate_RejectsASecurityKeyShorterThan32Bytes()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.SecurityKey = new string('a', 31)));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("SecurityKey tiene que ocupar", StringComparison.Ordinal));
    }

    /// <summary>
    /// La clave se mide en bytes UTF-8, que es lo que cuenta HMAC-SHA256. Contada por
    /// caracteres, esta cadena de 31 letras acentuadas se rechazaría pese a ocupar 62 bytes.
    /// </summary>
    [Fact]
    public void Validate_AcceptsAKeyOf31AccentedCharactersBecauseItOccupies62Bytes()
    {
        var key = new string('á', 31);

        Assert.Equal(62, Encoding.UTF8.GetByteCount(key));

        var result = Validator.Validate(name: null, CreateOptions(options => options.SecurityKey = key));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsAnEmptyIssuer()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.ValidIssuer = string.Empty));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("ValidIssuer", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsAnEmptyAudience()
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.ValidAudience = string.Empty));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("ValidAudience", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsALifetimeThatIsNotPositive(int expireInMinutes)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.ExpireInMinutes = expireInMinutes));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("ExpireInMinutes", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsARefreshLifetimeThatIsNotPositive(int expireInDays)
    {
        var result = Validator.Validate(
            name: null, CreateOptions(options => options.RefreshTokenExpireInDays = expireInDays));

        Assert.Contains(
            result.Failures!,
            failure => failure.StartsWith("RefreshTokenExpireInDays", StringComparison.Ordinal));
    }

    /// <summary>
    /// Acumular es la razón de tener validador propio en lugar de anotaciones sueltas: un
    /// despliegue mal configurado ve la lista entera en el primer arranque.
    /// </summary>
    [Fact]
    public void Validate_ReportsEveryFailureAtOnce()
    {
        var result = Validator.Validate(name: null, new JwtOptions());

        Assert.Equal(4, result.Failures!.Count());
    }

    private static JwtOptions CreateOptions(Action<JwtOptions>? configure = null)
    {
        var options = new JwtOptions
        {
            SecurityKey = "una-clave-de-firma-de-32-caracteres",
            ValidIssuer = "https://membership.persiltech.test",
            ValidAudience = "persiltech-sample",
            ExpireInMinutes = 30
        };

        configure?.Invoke(options);

        return options;
    }
}
