namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de cambio de correo de la cuenta autenticada.
/// </summary>
public sealed record ChangeEmailRequest
{
    /// <summary>Correo nuevo, al que se envía la confirmación.</summary>
    [Required]
    [EmailAddress]
    public string? NewEmail { get; init; }
}

