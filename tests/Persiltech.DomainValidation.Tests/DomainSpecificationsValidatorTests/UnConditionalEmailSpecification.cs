namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class UnConditionalEmailSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public UnConditionalEmailSpecification()
    {
        Property(u => u.Email)
            .IsRequired(UserRegistration.IsRequiredErrorMessage);
    }
}

