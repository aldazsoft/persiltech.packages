namespace Persiltech.DomainValidation.Tests.DomainSpecificationsTests;

public class SpecificationStopOnFirstErrorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task ValidateAsync_ShouldReturnExpectedResult_WhenValidate(
        IDomainSpecification<UserRegistration> specification,
        UserRegistration entity,
        bool expectedResult,
        IEnumerable<SpecificationError> expectedErrors)
    {
        // Act
        var errors = await specification.ValidateAsync(entity);

        // Assert
        Assert.Equal(expectedResult, errors.Count == 0);

        if (errors.Count != 0)
        {
            var expectedErrorsOrdered =
                expectedErrors.OrderBy(e => e.PropertyName)
                .ThenBy(e => e.ErrorMessage);
            var actualErrorsOrdered =
                errors.OrderBy(e => e.PropertyName)
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
            Assert.Empty(errors);
        }
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[]
        {
            new SpecificationWithoutStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "name@hotmail.com",
                Password = "Mm1234567$",
                ConfirmPassword="Mm1234567$"
            },
            true,
            new List<SpecificationError>()
        };

        yield return new object[]
        {
            new SpecificationWithoutStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "",
                Password = "123",
                ConfirmPassword = "456"
            },
            false,
            new List<SpecificationError>()
            {
                new SpecificationError("Email",
                    UserRegistration.IsRequiredErrorMessage),
                new SpecificationError("Password",
                    UserRegistration.HasMinLengthErrorMessage),
                new SpecificationError("ConfirmPassword",
                  UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage),
            }
        };

        yield return new object[]
        {
            new SpecificationWithStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "name@hotmail.com",
                Password = "Mm1234567$",
                ConfirmPassword="Mm1234567$"
            },
            true,
            new List<SpecificationError>()
        };

        yield return new object[]
        {
            new SpecificationWithStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "",
                Password = "123",
                ConfirmPassword="456"
            },
            false,
            new List<SpecificationError>()
            {
                new SpecificationError("Email",
                UserRegistration.IsRequiredErrorMessage),
            }
        };

        yield return new object[]
        {
            new SpecificationWithStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "name@hotmail.com",
                Password = "123",
                ConfirmPassword="456"
            },
            false,
            new List<SpecificationError>()
            {
                new SpecificationError("Password",
                    UserRegistration.HasMinLengthErrorMessage),
            }
        };

        yield return new object[]
        {
            new SpecificationWithStopOnFirstErrorSpecification(),
            new UserRegistration()
            {
                Email = "name@hotmail.com",
                Password = "Mm1234567$",
                ConfirmPassword="$Mm1234567"
            },
            false,
            new List<SpecificationError>()
            {
                new SpecificationError("ConfirmPassword",
                  UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage),
            }
        };
    }
}

