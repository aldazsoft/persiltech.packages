namespace Persiltech.DomainValidation.Tests.PropertySpecificationTreeExtensionsTests;

public class NotEmptyTests
{
    static Expression<Func<CreateOrder, IEnumerable<CreateOrderDetail>>>
        PropertyExpression = x => x.OrderDetails;

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task NotEmpty_ShouldReturnExpectedResult_WhenValueIsChecked(
        IEnumerable<CreateOrderDetail>? details, bool expectedResult)
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder,
            IEnumerable<CreateOrderDetail>>(PropertyExpression);

        tree.NotEmpty();

        var entity = new CreateOrder { OrderDetails = details! };

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
    public async Task NotEmpty_ShouldUseProvidedErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder,
            IEnumerable<CreateOrderDetail>>(PropertyExpression);
        string expectedErrorMessage = CreateOrder.NotEmpty;

        tree.NotEmpty(expectedErrorMessage);

        var entity = new CreateOrder { OrderDetails = null! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task NotEmpty_ShouldUseDefaultErrorMessage_WhenValidationFails()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder,
            IEnumerable<CreateOrderDetail>>(PropertyExpression);
        string expectedErrorMessage = ErrorMessages.NotEmpty;

        tree.NotEmpty();

        var entity = new CreateOrder { OrderDetails = null! };

        // Act
        var errors = await tree.Specifications[0].EvaluateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Equal(expectedErrorMessage,
            errors.First().ErrorMessage);
    }

    public static IEnumerable<object?[]> GetTestData()
    {
        yield return new object?[]
        {
            null, false
        };

        yield return new object?[]
        {
            new List<CreateOrderDetail>(), false
        };

        yield return new object?[]
        {
            new List<CreateOrderDetail>(){new() }, true
        };
    }
}

