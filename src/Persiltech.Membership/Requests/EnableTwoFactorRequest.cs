namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la activación del doble factor.
/// </summary>
public sealed record EnableTwoFactorRequest
{
    /// <summary>
    /// Código que muestra la aplicación de autenticación, con el que se demuestra que la
    /// clave compartida quedó bien registrada.
    /// </summary>
    [Required]
    public string? Code { get; init; }
}
