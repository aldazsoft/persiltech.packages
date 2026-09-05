namespace Persiltech.DomainValidation.Tests.DomainSpecificationsTests;

internal class SpecificationWithStopOnFirstErrorSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public SpecificationWithStopOnFirstErrorSpecification()
        : base(stopOnFirstEntitySpecificationError: true)
    {

        Property(e => e.Email)
           .IsRequired(UserRegistration.IsRequiredErrorMessage);

        Property(u => u.Password)
            .HasMinLength(6, UserRegistration.HasMinLengthErrorMessage);

        Property(u => u.ConfirmPassword)
            .Equal(u => u.Password,
            UserRegistration.PasswordConfirmationDoesNotMatchErrorMessage);
    }
}

