namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class ConditionalEmailSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public ConditionalEmailSpecification() : base(true)
    {
        Property(u => u.Email)
            .IsRequired(UserRegistration.IsRequiredErrorMessage);
    }
}

