namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class GreaterThanOrEqualToTests
{
    Expression<Func<CreateOrderDetail, int>> PropertyExpression =
        (x => x.ProductId);

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    public async Task GreaterThanOrEqualTo_ShouldReturnExpectedResult_WhenValueIsChecked(
        int productId, int comparisonValue, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<CreateOrderDetail, int>(
            PropertyExpression);
        tree.GreaterThanOrEqualTo(comparisonValue);

        var entity = new CreateOrderDetail { ProductId = productId };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.Equal(expectedResult, errors.Count == 0);

        if (!expectedResult)
        {
            Assert.Single(errors);
        }
        else
        {
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task GreaterThanOrEqualTo_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrderDetail, int>(
            PropertyExpression);
        string expectedMessage = CreateOrderDetail.ProductIdMessage;

        tree.GreaterThanOrEqualTo(1, expectedMessage);

        var entity = new CreateOrderDetail { ProductId = 0 };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task GreaterThanOrEqualTo_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrderDetail, int>(
            PropertyExpression);
        string expectedMessage =
            string.Format(ErrorMessages.GreaterThanOrEqualTo, 1);

        tree.GreaterThanOrEqualTo(1);

        var entity = new CreateOrderDetail { ProductId = 0 };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }

    // Antes de 1.0.3 la comparación convertía el valor con Convert.ChangeType, así que un
    // valor nulo terminaba en NullReferenceException en lugar de en un error de validación.
    [Fact]
    public async Task GreaterThanOrEqualTo_ShouldAddAnError_WhenTheValueIsNull()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            x => x.CustomerId);

        tree.GreaterThanOrEqualTo("ALFKI");

        var entity = new CreateOrder { CustomerId = null! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Single(errors);
    }
}
