namespace Persiltech.DomainValidation.Tests.CoreTests;

public class PropertySpecificationsTreeTests
{
    [Fact]
    public void Constructor_ShouldResolvePropertyName_WhenExpressionSelectsAMember()
    {
        // Arrange
        Expression<Func<CreateOrder, string>> propertyExpression =
            x => x.CustomerId;

        // Act
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            propertyExpression);

        // Assert
        Assert.Equal(nameof(CreateOrder.CustomerId), tree.PropertyName);
    }

    [Fact]
    public void Constructor_ShouldResolvePropertyName_WhenExpressionConvertsTheMember()
    {
        // Arrange
        Expression<Func<CreateOrderDetail, object>> propertyExpression =
            x => x.ProductId;

        // Act
        var tree = new PropertySpecificationsTree<CreateOrderDetail, object>(
            propertyExpression);

        // Assert
        Assert.Equal(nameof(CreateOrderDetail.ProductId), tree.PropertyName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenExpressionDoesNotSelectAMember()
    {
        // Arrange
        Expression<Func<CreateOrder, string>> propertyExpression =
            x => x.CustomerId.Trim();

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
            new PropertySpecificationsTree<CreateOrder, string>(
                propertyExpression));

        // Assert
        Assert.Equal("propertyExpression", exception.ParamName);
    }
}
