namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo del reinicio de contraseña con el testigo recibido por correo.
/// </summary>
public sealed record ResetPasswordRequest
{
    /// <summary>Correo de la cuenta.</summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>Testigo de reinicio que se envió al correo.</summary>
    [Required]
    public string? Token { get; init; }

    /// <summary>Contraseña nueva. La política la pone ASP.NET Core Identity.</summary>
    [Required]
    public string? NewPassword { get; init; }
}

