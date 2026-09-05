namespace Persiltech.DomainValidation.Tests.ResourcesTests;

public class ErrorMessagesLocalizationTests : IDisposable
{
    readonly CultureInfo OriginalCulture = CultureInfo.CurrentUICulture;

    // Solo hay dos traducciones —la neutra, en inglés, y la española—, así que cualquier
    // otra cultura debe caer en la neutra en lugar de devolver la clave del recurso.
    [Theory]
    [InlineData("es")]
    [InlineData("es-PE")]
    [InlineData("es-MX")]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("pt-BR")]
    public void ErrorMessages_ShouldResolveEveryMessage_WhenTheCultureHasNoTranslation(
        string culture)
    {
        // Arrange
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        string[] messages = [
            ErrorMessages.EmailAddress,
            ErrorMessages.Equal,
            ErrorMessages.GreaterThan,
            ErrorMessages.GreaterThanOrEqualTo,
            ErrorMessages.HasFixedLength,
            ErrorMessages.HasMaxLength,
            ErrorMessages.HasMinLength,
            ErrorMessages.IsRequired,
            ErrorMessages.Matches,
            ErrorMessages.NotEmpty];

        string[] resourceKeys = [
            nameof(ErrorMessages.EmailAddress),
            nameof(ErrorMessages.Equal),
            nameof(ErrorMessages.GreaterThan),
            nameof(ErrorMessages.GreaterThanOrEqualTo),
            nameof(ErrorMessages.HasFixedLength),
            nameof(ErrorMessages.HasMaxLength),
            nameof(ErrorMessages.HasMinLength),
            nameof(ErrorMessages.IsRequired),
            nameof(ErrorMessages.Matches),
            nameof(ErrorMessages.NotEmpty)];

        // Act & Assert
        for (int index = 0; index < messages.Length; index++)
        {
            Assert.False(string.IsNullOrWhiteSpace(messages[index]));
            Assert.NotEqual(resourceKeys[index], messages[index]);
        }
    }

    [Theory]
    [InlineData("es", "Esta información es requerida.")]
    [InlineData("es-PE", "Esta información es requerida.")]
    [InlineData("es-MX", "Esta información es requerida.")]
    [InlineData("en-GB", "This information is required.")]
    [InlineData("pt-BR", "This information is required.")]
    public void ErrorMessages_ShouldFallBackToTheNearestTranslation_WhenTheCultureIsSet(
        string culture, string expectedMessage)
    {
        // Arrange
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        // Act
        string message = ErrorMessages.IsRequired;

        // Assert
        Assert.Equal(expectedMessage, message);
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = OriginalCulture;

        GC.SuppressFinalize(this);
    }
}
