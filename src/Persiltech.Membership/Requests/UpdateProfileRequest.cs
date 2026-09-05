namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la actualización del perfil de la cuenta autenticada.
/// </summary>
/// <remarks>
/// No incluye el correo: cambiarlo exige confirmar la dirección nueva y va por
/// <see cref="EmailEndpoints"/>.
/// </remarks>
public sealed record UpdateProfileRequest
{
    /// <summary>Nombre del usuario.</summary>
    [Required]
    [MaxLength(100)]
    public string? FirstName { get; init; }

    /// <summary>Apellido del usuario.</summary>
    [Required]
    [MaxLength(100)]
    public string? LastName { get; init; }
}
