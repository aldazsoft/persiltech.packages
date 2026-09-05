namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class HasMaxLengthTests
{
    Expression<Func<CreateOrder, string>> PropertyExpression =
        (x => x.CustomerId);

    [Theory]
    [InlineData("ALFKIS", 5, false)]
    [InlineData(null, 5, true)]
    [InlineData("", 5, true)]
    [InlineData("ALF", 5, true)]
    [InlineData("ALFKI", 5, true)]
    public async Task HasMaxLength_ShouldReturnExpectedResult_WhenValueIsChecked(
        string? customerId, int length, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        tree.HasMaxLength(length);

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
    public async Task HasMaxLength_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedMessage = CreateOrder.CustomerIdHasMaxLength;

        tree.HasMaxLength(5, expectedMessage);

        var entity = new CreateOrder { CustomerId = "ALFKIS" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task HasMaxLength_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedMessage = string.Format(ErrorMessages.HasMaxLength, 5);

        tree.HasMaxLength(5);

        var entity = new CreateOrder { CustomerId = "ALFKIS" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }
}

