namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Comprobaciones que se hacen antes de llamar a la API al cambiar una contraseña.
/// </summary>
/// <remarks>
/// La confirmación no puede validarla el servidor: solo recibe una contraseña, así que un error
/// de tecleo llegaría hasta la cuenta sin que nadie lo notase. Se comprueba aquí, antes de
/// enviar nada.
/// <para>
/// Vive fuera del componente para poder probarla sin montar la interfaz.
/// </para>
/// </remarks>
internal static class ResetPasswordValidation
{
    /// <summary>
    /// Revisa la contraseña y su confirmación.
    /// </summary>
    /// <param name="password">Lo que escribió la persona.</param>
    /// <param name="confirmation">Lo que escribió la segunda vez.</param>
    /// <returns>
    /// El motivo por el que no se puede enviar, bajo la clave del campo al que acusa, o
    /// <see langword="null"/> si todo está en orden.
    /// </returns>
    /// <remarks>
    /// Devuelve la misma forma que <c>ValidationProblemDetails</c> —la clave nombra el campo—
    /// para que el aviso se pinte bajo su campo por la misma vía que los del servidor. Quien
    /// lee el formulario no tiene por qué notar cuál de los dos lo generó.
    /// </remarks>
    internal static IReadOnlyDictionary<string, string[]>? Validate(string? password, string? confirmation)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Fail(NewPasswordKey, "Escribe la contraseña nueva.");
        }

        if (string.IsNullOrWhiteSpace(confirmation))
        {
            return Fail(ConfirmPasswordKey, "Repite la contraseña para confirmarla.");
        }

        // Ordinal y no cultural: una contraseña es una secuencia de caracteres, no un texto que
        // se compare según el idioma. Dos cadenas que una cultura consideraría iguales son
        // contraseñas distintas.
        return string.Equals(password, confirmation, StringComparison.Ordinal)
            ? null
            : Fail(ConfirmPasswordKey, "Las dos contraseñas no coinciden.");
    }

    /// <summary>Clave del campo de la contraseña nueva, la misma que usa la API.</summary>
    internal const string NewPasswordKey = "newPassword";

    /// <summary>Clave del campo de la confirmación, que solo existe en el navegador.</summary>
    internal const string ConfirmPasswordKey = "confirmPassword";

    private static Dictionary<string, string[]> Fail(string field, string message) =>
        new(StringComparer.Ordinal) { [field] = [message] };

    /// <summary>
    /// Si el correo llega del enlace y no debe poder cambiarse.
    /// </summary>
    /// <remarks>
    /// Bloquearlo siempre dejaría inservible la pantalla de quien entra a la ruta sin
    /// parámetros: se quedaría con un campo vacío que no puede rellenar.
    /// </remarks>
    /// <param name="emailFromLink">Correo recibido en la cadena de consulta.</param>
    /// <returns><see langword="true"/> si venía en el enlace.</returns>
    internal static bool ShouldLockEmail(string? emailFromLink) =>
        !string.IsNullOrWhiteSpace(emailFromLink);
}
