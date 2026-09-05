namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de activación o desactivación de una cuenta.
/// </summary>
/// <remarks>
/// La propiedad es anulable por la misma razón que el resto de los cuerpos de petición del
/// paquete: un campo ausente llega como <see langword="null"/> y lo rechaza
/// <see cref="RequiredAttribute"/>.
/// </remarks>
public sealed record UpdateUserStatusRequest
{
    /// <summary>
    /// Estado al que pasa la cuenta.
    /// </summary>
    [Required]
    public bool? IsActive { get; init; }
}
