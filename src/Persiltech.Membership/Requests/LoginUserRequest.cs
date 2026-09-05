namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de autenticación.
/// </summary>
/// <remarks>
/// Las dos propiedades son anulables y no llevan <c>required</c> por la misma razón que en
/// <see cref="RegisterUserRequest"/>: el campo ausente tiene que llegar como
/// <see langword="null"/> para que el error salga como <c>ValidationProblemDetails</c>.
/// </remarks>
public sealed record LoginUserRequest
{
    /// <summary>
    /// Correo de la cuenta con la que se autentica.
    /// </summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// Contraseña de la cuenta.
    /// </summary>
    [Required]
    public string? Password { get; init; }

    /// <summary>
    /// Segundo factor: el código de la aplicación de autenticación o uno de recuperación.
    /// </summary>
    /// <remarks>
    /// Solo se exige en cuentas con el doble factor activado, y por eso no lleva
    /// <see cref="RequiredAttribute"/>. Viajar aquí, en lugar de partir la autenticación en
    /// dos llamadas, mantiene intacto el contrato de la respuesta: quien ya consumía este
    /// endpoint no cambia nada mientras no active el doble factor.
    /// </remarks>
    public string? TwoFactorCode { get; init; }
}
