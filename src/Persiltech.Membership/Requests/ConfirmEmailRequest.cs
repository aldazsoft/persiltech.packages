namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la confirmación del correo con el testigo recibido.
/// </summary>
public sealed record ConfirmEmailRequest
{
    /// <summary>Correo de la cuenta.</summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>Testigo de confirmación que se envió al correo.</summary>
    [Required]
    public string? Token { get; init; }
}

