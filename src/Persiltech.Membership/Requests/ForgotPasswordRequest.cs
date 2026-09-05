namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de reinicio de una contraseña olvidada.
/// </summary>
public sealed record ForgotPasswordRequest
{
    /// <summary>Correo de la cuenta.</summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }
}

