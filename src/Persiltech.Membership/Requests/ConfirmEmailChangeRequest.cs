namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la confirmación de un cambio de correo.
/// </summary>
public sealed record ConfirmEmailChangeRequest
{
    /// <summary>Correo nuevo, el mismo que se pidió en el cambio.</summary>
    [Required]
    [EmailAddress]
    public string? NewEmail { get; init; }

    /// <summary>Testigo que se envió al correo nuevo.</summary>
    [Required]
    public string? Token { get; init; }
}

