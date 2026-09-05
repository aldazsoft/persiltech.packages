namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class UnConditionalConfirmPasswordSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public UnConditionalConfirmPasswordSpecification()
    {
        Property(u => u.ConfirmPassword)
            .Equal(u => u.Password,
                UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage);
    }
}

