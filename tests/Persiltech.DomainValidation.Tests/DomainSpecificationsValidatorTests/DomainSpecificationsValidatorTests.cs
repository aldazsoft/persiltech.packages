namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

public class DomainSpecificationsValidatorTests
{
    [Theory]
    [MemberData(nameof(TestData.GetTestData),
        MemberType = typeof(TestData))]
    public async Task ValidateAsync_ShouldReturnExpectedResult_WhenValidateDomainSpecifications(
        IDomainSpecification<UserRegistration>[] specifications,
        UserRegistration user,
        ValidationResult expectedResult)
    {
        // Arrange
        IDomainSpecificationsValidator<UserRegistration> validator =
            new DomainSpecificationsValidator<UserRegistration>(specifications);

        // Act
        var result = await validator.ValidateAsync(user, TestContext.Current.CancellationToken);

        // Arrange
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

