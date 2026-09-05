namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class IsRequiredTests
{
    Expression<Func<CreateOrder, string>> PropertyExpression =
        x => x.CustomerId;

    [Theory]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("ALFKI", true)]
    public async Task IsRequired_ShouldReturnExpectedResult_WhenValueIsChecked(
        string? customerId, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);

        tree.IsRequired();

        var entity = new CreateOrder { CustomerId = customerId! };

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
    public async Task IsRequired_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedErrorMessage = CreateOrder.CustomerIdIsRequired;

        tree.IsRequired(expectedErrorMessage);

        var entity = new CreateOrder { CustomerId = null! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task IsRequired_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedErrorMessage = ErrorMessages.IsRequired;

        tree.IsRequired();

        var entity = new CreateOrder { CustomerId = null! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

}

