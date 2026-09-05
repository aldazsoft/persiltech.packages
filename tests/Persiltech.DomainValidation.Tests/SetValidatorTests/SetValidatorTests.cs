namespace Persiltech.DomainValidation.Tests.SetValidatorTests;

public class SetValidatorTests
{
    [Theory]
    [MemberData(nameof(SetValidatorTestData.GetTestData),
    MemberType = typeof(SetValidatorTestData))]
    public async Task ValidateAsync_ShouldReturnExpectedResult_WhenValidateDomainSpecifications(
        CreateOrder order,
        ValidationResult expectedResult)
    {
        // Arrange
        IDomainSpecificationsValidator<CreateOrderDetail> orderDetailValidator =
            new DomainSpecificationsValidator<CreateOrderDetail>([
                new CreateOrderDetailSpecification()
                ]);

        IDomainSpecificationsValidator<CreateOrder> validator =
            new DomainSpecificationsValidator<CreateOrder>([
                new CreateOrderSpecification(orderDetailValidator)
                ]);

        // Act
        var result = await validator.ValidateAsync(order);

        // Assert
        Assert.Equal(expectedResult.IsValid, result.IsValid);

        if (result.IsValid == false)
        {
            var expectedErrorsOrdered =
                expectedResult.Errors.OrderBy(e => e.PropertyName)
                .ThenBy(e => e.ErrorMessage);
            var actualErrorsOrdered =
                result.Errors.OrderBy(e => e.PropertyName)
                .ThenBy(e => e.ErrorMessage);

            Assert.Collection(
                actualErrorsOrdered,
                expectedErrorsOrdered
                .Select(expected => (Action<SpecificationError>)(actual =>
                {
                    Assert.Equal(expected.PropertyName, actual.PropertyName);
                    Assert.Equal(expected.ErrorMessage, actual.ErrorMessage);
                })
                ).ToArray()
            );
        }
        else
        {
            Assert.True(result.Errors == null || !result.Errors.Any());
        }
    }
}

