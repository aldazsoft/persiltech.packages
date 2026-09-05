namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de renombrado de un rol.
/// </summary>
/// <remarks>
/// La propiedad es anulable y no lleva <c>required</c> por la misma razón que el resto de
/// los cuerpos de petición del paquete.
/// </remarks>
public sealed record UpdateRoleRequest
{
    /// <summary>
    /// Nuevo nombre del rol. Obligatorio, hasta 256 caracteres.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string? Name { get; init; }
}
