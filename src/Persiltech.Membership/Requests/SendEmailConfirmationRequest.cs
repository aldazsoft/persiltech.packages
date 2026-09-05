namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición que reenvía la confirmación del correo.
/// </summary>
public sealed record SendEmailConfirmationRequest
{
    /// <summary>Correo de la cuenta.</summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }
}

