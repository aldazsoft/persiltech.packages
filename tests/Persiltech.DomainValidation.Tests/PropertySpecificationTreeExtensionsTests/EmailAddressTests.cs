namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class EmailAddressTests
{
    Expression<Func<UserRegistration, string>> PropertyExpression =
        x => x.Email;

    [Theory]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("@hotmail.com", false)]
    [InlineData("name@hotmail", false)]
    [InlineData(".com", false)]
    [InlineData("name@hotmail.com", true)]
    public async Task EmailAddress_ShouldReturnExpectedResult_WhenValueIsChecked(
        string? email, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);
        tree.EmailAddress();

        var entity = new UserRegistration { Email = email! };

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
    public async Task EmailAddress_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);
        string expectedErrorMessage = UserRegistration.EmailErrorMessage;

        tree.EmailAddress(expectedErrorMessage);

        var entity = new UserRegistration { Email = "name" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task EmailAddress_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);
        string expectedErrorMessage = ErrorMessages.EmailAddress;

        tree.EmailAddress();

        var entity = new UserRegistration { Email = "name" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }
}

