namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Reglas que se comprueban antes de mandar la contraseña nueva a la API.
/// </summary>
public class ResetPasswordValidationTests
{
    [Fact]
    public void AcceptsTwoIdenticalPasswords()
    {
        Assert.Null(ResetPasswordValidation.Validate("Passw0rd!", "Passw0rd!"));
    }

    /// <summary>
    /// El caso que justifica todo esto: el servidor recibe una sola contraseña, así que un error
    /// de tecleo llegaría hasta la cuenta sin que nadie lo notase.
    /// </summary>
    [Fact]
    public void RejectsTwoPasswordsThatDiffer()
    {
        var error = ResetPasswordValidation.Validate("Passw0rd!", "Passw0rd?");

        Assert.Equal("Las dos contraseñas no coinciden.", error);
    }

    /// <summary>
    /// Se comparan carácter a carácter, no según el idioma: dos cadenas que una cultura daría
    /// por iguales son contraseñas distintas para quien las guarda.
    /// </summary>
    [Theory]
    [InlineData("Passw0rd!", "passw0rd!")]
    [InlineData("Contraseña1!", "Contraseña1!")]
    public void ComparesTheCharactersAndNotTheCulture(string password, string confirmation)
    {
        Assert.NotNull(ResetPasswordValidation.Validate(password, confirmation));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsAnEmptyPassword(string? password)
    {
        Assert.Equal("Escribe la contraseña nueva.", ResetPasswordValidation.Validate(password, "Passw0rd!"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsAnEmptyConfirmation(string? confirmation)
    {
        Assert.Equal(
            "Repite la contraseña para confirmarla.",
            ResetPasswordValidation.Validate("Passw0rd!", confirmation));
    }

    /// <summary>
    /// Los espacios cuentan: una contraseña que empieza por espacio no es la misma que sin él.
    /// </summary>
    [Fact]
    public void DoesNotTrimThePasswords()
    {
        Assert.NotNull(ResetPasswordValidation.Validate(" Passw0rd!", "Passw0rd!"));
    }

    [Fact]
    public void LocksTheEmailWhenItCameInTheLink()
    {
        Assert.True(ResetPasswordValidation.ShouldLockEmail("juan.perez@example.com"));
    }

    /// <summary>
    /// Sin correo en el enlace el campo tiene que quedar editable: bloquearlo vacío dejaría la
    /// pantalla inservible para quien entra a la ruta a pelo.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LeavesTheEmailEditableWhenTheLinkDidNotCarryIt(string? emailFromLink)
    {
        Assert.False(ResetPasswordValidation.ShouldLockEmail(emailFromLink));
    }
}
