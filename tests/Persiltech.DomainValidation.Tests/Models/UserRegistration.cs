namespace Persiltech.DomainValidation.Tests.Models;

public class UserRegistration
{
    public const string EmailErrorMessage =
        "Invalid email format";
    public const string IsRequiredErrorMessage =
        "Required data";
    public const string HasMinLengthErrorMessage =
        "At least 6 characters are required";
    public const string UppercaseCharactersAreRequiredErrorMessage =
        "Capital characters are required";
    public const string LowercaseCharactersAreRequiredErrorMessage =
        "Lowercase characters are required";
    public const string DigitsAreRequiredErrorMessage =
        "Digits are required";
    public const string SymbolsAreRequiredErrorMessage =
        "Symbols are required";
    public const string PasswordConfirmationDoesNotMatchErrorMessage =
        "Password confirmation does not match";

    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

