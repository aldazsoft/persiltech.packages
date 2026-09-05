namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class ConditionalPasswordSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public ConditionalPasswordSpecification() : base(true)
    {
        Property(u => u.Password)
            .HasMinLength(6, UserRegistration.HasMinLengthErrorMessage);
    }
}

