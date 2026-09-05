namespace Persiltech.Membership.Tests;

public class RequestValidationTests
{
    [Fact]
    public void TryValidateAcceptsACompleteRequest()
    {
        var request = new RegisterUserRequest
        {
            Email = "juan.perez@example.com",
            Password = "Passw0rd!",
            FirstName = "Juan",
            LastName = "Pérez"
        };

        Assert.True(RequestValidation.TryValidate(request, out var errors));
        Assert.Null(errors);
    }

    [Fact]
    public void TryValidateNamesEveryMissingFieldInCamelCase()
    {
        Assert.False(RequestValidation.TryValidate(new RegisterUserRequest(), out var errors));
        Assert.Equal(["email", "firstName", "lastName", "password"], errors.Keys.Order());
    }

    [Fact]
    public void TryValidateRejectsAMalformedEmail()
    {
        var request = new LoginUserRequest { Email = "no-es-un-correo", Password = "Passw0rd!" };

        Assert.False(RequestValidation.TryValidate(request, out var errors));
        Assert.Equal("email", Assert.Single(errors.Keys));
        Assert.NotEmpty(Assert.Single(errors.Values));
    }

    [Fact]
    public void TryValidatePutsTheErrorsWithoutAFieldUnderTheEmptyKey()
    {
        Assert.False(RequestValidation.TryValidate(new FormLevelRequest(), out var errors));

        var error = Assert.Single(errors);

        Assert.Equal(string.Empty, error.Key);
        Assert.Equal(["Error de formulario."], error.Value);
    }

    private sealed record FormLevelRequest : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            [new ValidationResult("Error de formulario.")];
    }
}
