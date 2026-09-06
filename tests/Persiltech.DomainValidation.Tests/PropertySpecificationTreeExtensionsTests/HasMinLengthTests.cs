namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class HasMinLengthTests
{
    Expression<Func<CreateOrder, string>> PropertyExpression =
        (x => x.CustomerId);

    [Theory]
    [InlineData(null, 5, false)]
    [InlineData("", 5, false)]
    [InlineData("ALF", 5, false)]
    [InlineData("ALFKI", 5, true)]
    [InlineData("ALFKIS", 5, true)]
    public async Task HasMinLength_ShouldReturnExpectedResult_WhenValueIsChecked(
        string? customerId, int length, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        tree.HasMinLength(length);

        var entity = new CreateOrder { CustomerId = customerId! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

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
    public async Task HasMinLength_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedMessage = CreateOrder.CustomerIdHasMinLength;

        tree.HasMinLength(5, expectedMessage);

        var entity = new CreateOrder { CustomerId = "ALF" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task HasMinLength_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedMessage = string.Format(ErrorMessages.HasMinLength, 5);

        tree.HasMinLength(5);

        var entity = new CreateOrder { CustomerId = "ALF" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }
}

