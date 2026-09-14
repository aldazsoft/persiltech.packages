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
    /// El motivo por el que no se puede enviar, o <see langword="null"/> si todo está en orden.
    /// </returns>
    internal static string? Validate(string? password, string? confirmation)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Escribe la contraseña nueva.";
        }

        if (string.IsNullOrWhiteSpace(confirmation))
        {
            return "Repite la contraseña para confirmarla.";
        }

        // Ordinal y no cultural: una contraseña es una secuencia de caracteres, no un texto que
        // se compare según el idioma. Dos cadenas que una cultura consideraría iguales son
        // contraseñas distintas.
        return string.Equals(password, confirmation, StringComparison.Ordinal)
            ? null
            : "Las dos contraseñas no coinciden.";
    }

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
