namespace Persiltech.DomainValidation.Tests.ValueObjectsTests;

public class ValidationResultTests
{
    [Fact]
    public void Constructor_ShouldEnumerateTheSequenceOnce_WhenTheResultIsQueriedSeveralTimes()
    {
        // Arrange
        int enumerationCount = 0;

        // Act
        var result = new ValidationResult(
            EnumerateOnce(() => enumerationCount++));

        _ = result.IsValid;
        _ = result.IsValid;
        _ = result.Errors.Count();

        // Assert
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public void IsValid_ShouldBeTrue_WhenThereAreNoErrors()
    {
        // Arrange
        var result = new ValidationResult([]);

        // Act & Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void IsValid_ShouldBeFalse_WhenThereAreErrors()
    {
        // Arrange
        var result = new ValidationResult(
            [new SpecificationError("CustomerId", CreateOrder.CustomerIdIsRequired)]);

        // Act & Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    static IEnumerable<SpecificationError> EnumerateOnce(Action onEnumerate)
    {
        onEnumerate();

        yield return new SpecificationError(
            "CustomerId", CreateOrder.CustomerIdIsRequired);
    }
}
