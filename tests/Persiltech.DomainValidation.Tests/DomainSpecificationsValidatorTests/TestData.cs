namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal static class TestData
{
    public static IEnumerable<Object[]> GetTestData()
    {
        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification()
            },
            new UserRegistration
            {
                Email= "name@hotmail.com"
            },
            new ValidationResult([])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification()
            },
            new UserRegistration
            {
                Email= ""
            },
            new ValidationResult([
                new SpecificationError("Email",
                    UserRegistration.IsRequiredErrorMessage)
                ])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new UnConditionalPasswordSpecification(),
                new UnConditionalConfirmPasswordSpecification()
            },
            new UserRegistration
            {
                Email= "name@hotmail.com",
                Password="Mm1234567$",
                ConfirmPassword="Mm1234567$"
            },
            new ValidationResult([])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new UnConditionalPasswordSpecification(),
                new UnConditionalConfirmPasswordSpecification()
            },
            new UserRegistration
            {
                Email= null!,
                Password="123",
                ConfirmPassword="456"
            },
            new ValidationResult([
                new SpecificationError("Email",
                    UserRegistration.IsRequiredErrorMessage),
                new SpecificationError("Password",
                    UserRegistration.HasMinLengthErrorMessage),
                new SpecificationError("ConfirmPassword",
                   UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage)
                ])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new ConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= "name@hotmail.com",
                Password="Mm1234567$"
            },
            new ValidationResult([])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new ConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= null!,
                Password="123"
            },
            new ValidationResult([
                new SpecificationError("Email",
                    UserRegistration.IsRequiredErrorMessage)
                ])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= "name@hotmail.com",
                Password="Mm1234567$"
            },
            new ValidationResult([])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= null!,
                Password="123"
            },
            new ValidationResult([
                new SpecificationError("Email",
                    UserRegistration.IsRequiredErrorMessage)
                ])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= ConditionalUniqueEmailSpecification.TestEmail,
                Password="123"
            },
            new ValidationResult([
                new SpecificationError("Password",
                    UserRegistration.HasMinLengthErrorMessage)
                ])
        };

        yield return new Object[]
        {
            new IDomainSpecification<UserRegistration>[]
            {
                new UnConditionalEmailSpecification(),
                new ConditionalPasswordSpecification(),
                new ConditionalUniqueEmailSpecification()
            },
            new UserRegistration
            {
                Email= ConditionalUniqueEmailSpecification.TestEmail,
                Password="Mm1234567$"
            },
            new ValidationResult([
                new SpecificationError("Email",
                ConditionalUniqueEmailSpecification.DuplicateEmailMessage)
                ])
        };
    }
}

