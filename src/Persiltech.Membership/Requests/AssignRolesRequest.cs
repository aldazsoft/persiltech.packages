namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición que fija los roles de un usuario.
/// </summary>
/// <remarks>
/// La lista <em>sustituye</em> a la anterior: el usuario queda exactamente con los roles
/// indicados. Un arreglo vacío es válido y lo deja sin ninguno.
/// </remarks>
public sealed record AssignRolesRequest
{
    /// <summary>
    /// Nombres de los roles que tendrá el usuario.
    /// </summary>
    [Required]
    public string[]? Roles { get; init; }
}
