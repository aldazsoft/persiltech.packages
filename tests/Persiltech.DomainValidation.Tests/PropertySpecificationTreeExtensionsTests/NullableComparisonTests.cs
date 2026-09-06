namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

// Antes de la 2.0.0 estas reglas no compilaban sobre una propiedad anulable por valor: la
// restricción IComparable<T> no la admite. Las sobrecargas para `struct` cierran ese hueco.
public class NullableComparisonTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task GreaterThan_ShouldReturnExpectedResult_WhenThePropertyIsNullable(
        int? quantity, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<StockAdjustment, int?>(
            x => x.Quantity);

        tree.GreaterThan(0);

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(
            new StockAdjustment { Quantity = quantity }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, errors.Count == 0);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task GreaterThanOrEqualTo_ShouldReturnExpectedResult_WhenThePropertyIsNullable(
        int? quantity, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<StockAdjustment, int?>(
            x => x.Quantity);

        tree.GreaterThanOrEqualTo(1);

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(
            new StockAdjustment { Quantity = quantity }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, errors.Count == 0);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(5, 5, true)]
    [InlineData(null, 5, false)]
    [InlineData(5, null, false)]
    [InlineData(5, 6, false)]
    public async Task Equal_ShouldReturnExpectedResult_WhenBothPropertiesAreNullable(
        int? quantity, int? confirmedQuantity, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<StockAdjustment, int?>(
            x => x.Quantity);

        tree.Equal(x => x.ConfirmedQuantity);

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(
            new StockAdjustment
            {
                Quantity = quantity,
                ConfirmedQuantity = confirmedQuantity
            }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, errors.Count == 0);
    }

    [Fact]
    public async Task GreaterThan_ShouldReturnExpectedResult_WhenThePropertyIsNullableDecimal()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<StockAdjustment, decimal?>(
            x => x.UnitPrice);

        tree.GreaterThan(0m);

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(
            new StockAdjustment { UnitPrice = 0.01m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(errors);
    }
}
