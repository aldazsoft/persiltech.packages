namespace Persiltech.DomainValidation.Tests.DomainSpecificationsTests;

internal class PropertyWithStopOnFirstErrorSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public PropertyWithStopOnFirstErrorSpecification()
    {
        Property(u => u.Password, true)
            .HasMinLength(6, UserRegistration.HasMinLengthErrorMessage)
            .Must(p => p.Any(c => char.IsUpper(c)),
                UserRegistration.UppercaseCharactersAreRequiredErrorMessage)
            .Must(p => p.Any(c => char.IsLower(c)),
                UserRegistration.LowercaseCharactersAreRequiredErrorMessage)
            .Must(p => p.Any(c => char.IsDigit(c)),
                UserRegistration.DigitsAreRequiredErrorMessage)
            .Must(p => p.Any(c => !char.IsLetterOrDigit(c)),
                UserRegistration.SymbolsAreRequiredErrorMessage);
    }
}

