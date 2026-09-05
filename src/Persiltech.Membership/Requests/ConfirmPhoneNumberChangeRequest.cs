namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la confirmación de un cambio de teléfono.
/// </summary>
public sealed record ConfirmPhoneNumberChangeRequest
{
    /// <summary>Teléfono nuevo, el mismo que se pidió en el cambio.</summary>
    [Required]
    [Phone]
    public string? PhoneNumber { get; init; }

    /// <summary>Código que se envió por SMS.</summary>
    [Required]
    public string? Token { get; init; }
}
