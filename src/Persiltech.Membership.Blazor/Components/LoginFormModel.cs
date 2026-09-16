namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Lo que se escribe en <c>MembershipLoginForm</c>.
/// </summary>
/// <remarks>
/// Los nombres de las propiedades **no son decorativos**: son la clave por la que el error que
/// devuelve la API encuentra su campo. La API serializa <c>email</c>, <c>password</c> y
/// <c>twoFactorCode</c>, y <c>ApiValidator</c> los empareja con estas tres. Renombrar una aquí
/// sin renombrarla allí deja su mensaje sin campo donde pintarse.
/// </remarks>
internal sealed class LoginFormModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Eso no parece un correo.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string? Password { get; set; }

    /// <summary>
    /// Código del segundo factor, solo cuando la API lo pide.
    /// </summary>
    /// <remarks>
    /// Sin <c>Required</c>: el campo únicamente aparece si el intento anterior lo reclamó, y
    /// exigirlo siempre bloquearía a quien no tiene el doble factor activado.
    /// </remarks>
    public string? TwoFactorCode { get; set; }
}
