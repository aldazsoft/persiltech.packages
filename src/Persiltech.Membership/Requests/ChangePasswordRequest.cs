namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo del cambio de contraseña de la cuenta autenticada.
/// </summary>
/// <remarks>
/// Las propiedades son anulables y sin <c>required</c> por la misma razón que el resto de
/// los cuerpos de petición del paquete: un campo ausente llega como <see langword="null"/>
/// y lo rechaza <see cref="RequiredAttribute"/>, en lugar de fallar la deserialización.
/// </remarks>
public sealed record ChangePasswordRequest
{
    /// <summary>Contraseña actual de la cuenta.</summary>
    [Required]
    public string? CurrentPassword { get; init; }

    /// <summary>Contraseña nueva. La política la pone ASP.NET Core Identity.</summary>
    [Required]
    public string? NewPassword { get; init; }
}

