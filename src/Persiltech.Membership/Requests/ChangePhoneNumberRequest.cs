namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de cambio de teléfono de la cuenta autenticada.
/// </summary>
public sealed record ChangePhoneNumberRequest
{
    /// <summary>Teléfono nuevo, al que se envía el código.</summary>
    [Required]
    [Phone]
    public string? PhoneNumber { get; init; }
}

