namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class EqualTests
{
    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, true)]
    public async Task Equal_ShouldReturnExpectedResult_WhenNumericValueIsChecked(
        int productId, int comparisonValue, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<CreateOrderDetail, int>(
            x => x.ProductId);
        tree.Equal(comparisonValue!);

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

    [Theory]
    [InlineData(null, "ALFKI", false)]
    [InlineData("ALFKI", null, false)]
    [InlineData("ALF", "ALFKI", false)]
    [InlineData(null, null, true)]
    [InlineData("", "", true)]
    [InlineData("ALFKI", "ALFKI", true)]
    public async Task Equal_ShouldReturnExpectedResult_WhenStringValueIsChecked(
        string? customerId, string? comparisonValue, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            x => x.CustomerId);
        tree.Equal(comparisonValue!);

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

    [Theory]
    [InlineData(null, "Mm1234567$", false)]
    [InlineData("Mm1234567$", null, false)]
    [InlineData("Mm1234567$", "Mm1234567", false)]
    [InlineData(null, null, true)]
    [InlineData("", "", true)]
    [InlineData("Mm1234567$", "Mm1234567$", true)]
    public async Task Equal_ShouldReturnExpectedResult_WhenPropertiesAreChecked(
        string? password, string? confirmPassword, bool expectedResult)
    {
        // Arrange        
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            x => x.Password);

        tree.Equal(x => x.ConfirmPassword);

        var entity = new UserRegistration
        {
            Password = password!,
            ConfirmPassword = confirmPassword!
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
    public async Task Equal_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            x => x.ConfirmPassword);
        string expectedMessage =
            UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage;

        tree.Equal(x => x.Password, expectedMessage);

        var entity = new UserRegistration
        {
            Password = "Mm1234567$",
            ConfirmPassword = "Mm1234567"
        };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task Equal_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<UserRegistration, string>(
            x => x.ConfirmPassword);
        string expectedMessage = ErrorMessages.Equal;

        tree.Equal(x => x.Password);

        var entity = new UserRegistration
        {
            Password = "Mm1234567$",
            ConfirmPassword = "Mm1234567"
        };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedMessage,
            errors.First().ErrorMessage);
    }
}

