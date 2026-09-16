namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Reglas que se comprueban antes de mandar la contraseña nueva a la API.
/// </summary>
/// <remarks>
/// Devuelven la misma forma que <c>ValidationProblemDetails</c> —el mensaje bajo la clave del
/// campo al que acusa— para que el formulario los pinte por la misma vía que los del servidor.
/// De ahí que cada prueba compruebe también la clave, y no solo el texto.
/// </remarks>
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
        var errors = ResetPasswordValidation.Validate("Passw0rd!", "Passw0rd?");

        Assert.NotNull(errors);
        Assert.Equal(
            ["Las dos contraseñas no coinciden."],
            errors[ResetPasswordValidation.ConfirmPasswordKey]);
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

    /// <summary>
    /// Cada aviso nombra el campo que lo provocó, que es lo que lo lleva bajo su entrada.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsAnEmptyPassword(string? password)
    {
        var errors = ResetPasswordValidation.Validate(password, "Passw0rd!");

        Assert.NotNull(errors);
        Assert.Equal(
            ["Escribe la contraseña nueva."],
            errors[ResetPasswordValidation.NewPasswordKey]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsAnEmptyConfirmation(string? confirmation)
    {
        var errors = ResetPasswordValidation.Validate("Passw0rd!", confirmation);

        Assert.NotNull(errors);
        Assert.Equal(
            ["Repite la contraseña para confirmarla."],
            errors[ResetPasswordValidation.ConfirmPasswordKey]);
    }

    /// <summary>
    /// Un aviso acusa a un solo campo: el resto del formulario no debe teñirse de rojo.
    /// </summary>
    [Fact]
    public void BlamesASingleField()
    {
        var errors = ResetPasswordValidation.Validate("Passw0rd!", "Passw0rd?");

        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.False(errors.ContainsKey(ResetPasswordValidation.NewPasswordKey));
    }

    /// <summary>
    /// Las claves son las que serializa la API, no las propiedades en PascalCase.
    /// </summary>
    /// <remarks>
    /// El emparejamiento con el modelo ignora mayúsculas, así que ambas valdrían; se fijan en
    /// camelCase para que un aviso propio y uno del servidor sobre el mismo campo se escriban
    /// igual y nadie tenga que recordar cuál es cuál.
    /// </remarks>
    [Fact]
    public void UsesTheKeysThatTheApiSerializes()
    {
        Assert.Equal("newPassword", ResetPasswordValidation.NewPasswordKey);
        Assert.Equal("confirmPassword", ResetPasswordValidation.ConfirmPasswordKey);
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
