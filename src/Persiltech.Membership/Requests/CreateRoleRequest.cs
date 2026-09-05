namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de creación de un rol.
/// </summary>
/// <remarks>
/// La propiedad es anulable y no lleva <c>required</c> por la misma razón que el resto de
/// los cuerpos de petición del paquete: un campo ausente llega como
/// <see langword="null"/> y lo rechaza <see cref="RequiredAttribute"/>, en lugar de
/// fallar la deserialización con un error de forma distinta de la acordada.
/// </remarks>
public sealed record CreateRoleRequest
{
    /// <summary>
    /// Nombre del rol. Obligatorio, hasta 256 caracteres, que es el máximo de la columna
    /// <c>Name</c> de <c>AspNetRoles</c>.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string? Name { get; init; }
}
