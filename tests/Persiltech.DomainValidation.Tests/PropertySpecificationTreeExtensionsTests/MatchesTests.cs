namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class MatchesTests
{
    Expression<Func<CreateOrder, string>> PropertyExpression =
        x => x.CustomerId;

    [Theory]
    [InlineData(null, "^([0-9]{5})$", false)]
    [InlineData("", "^([0-9]{5})$", false)]
    [InlineData("123", "^([0-9]{5})$", false)]
    [InlineData("123456", "^([0-9]{5})$", false)]
    [InlineData("12345", "^([0-9]{5})$", true)]
    public async Task Matches_ShouldReturnExpectedResult_WhenValueIsChecked(
        string? customerId, string regularExpression, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        tree.Matches(regularExpression);

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
    public async Task Matches_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string expectedErrorMessage = CreateOrder.CustomerIdMatch;
        string regularExpression = "^([0-9]{5})$";

        tree.Matches(regularExpression, expectedErrorMessage);

        var entity = new CreateOrder { CustomerId = "" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task Matches_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            PropertyExpression);
        string regularExpression = "^([0-9]{5})$";
        string expectedErrorMessage =
            string.Format(ErrorMessages.Matches, regularExpression);

        tree.Matches(regularExpression);

        var entity = new CreateOrder { CustomerId = "" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }
}

