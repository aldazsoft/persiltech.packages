namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class UnConditionalPasswordSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public UnConditionalPasswordSpecification()
    {
        Property(u => u.Password)
            .HasMinLength(6, UserRegistration.HasMinLengthErrorMessage);
    }
}

