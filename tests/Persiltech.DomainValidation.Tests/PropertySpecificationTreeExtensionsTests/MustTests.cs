namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class MustTests
{
    Expression<Func<UserRegistration, string>> PropertyExpression =
        (x => x.Password);

    [Theory]
    [InlineData("", false)]
    [InlineData("12345", false)]
    [InlineData("M", true)]
    public async Task Must_ShouldReturnExpectedResult_WhenValueIsChecked(
        string passwordValue, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);
        tree.Must(p => p.Any(c => char.IsUpper(c)),
            UserRegistration.UppercaseCharactersAreRequiredErrorMessage);

        var entity = new UserRegistration { Password = passwordValue };

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

    [Theory]
    [InlineData("Admin", "Admin", false)]
    [InlineData("Admin@name.com", "Password", true)]
    public async Task Must_ShouldReturnExpectedResult_WhenEntityIsChecked(
        string email, string password, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);

        tree.Must((UserRegistration t) =>
            t.Email == "Admin@name.com" && t.Password == "Password",
            "Error");

        var entity = new UserRegistration
        {
            Email = email,
            Password = password
        };

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
    public async Task Must_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            PropertyExpression);

        string expectedMessage =
            UserRegistration.UppercaseCharactersAreRequiredErrorMessage;

        tree.Must(p => p.Any(c => char.IsUpper(c)), expectedMessage);

        var entity = new UserRegistration { Password = "12345" };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }
}

