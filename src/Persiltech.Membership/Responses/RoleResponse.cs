namespace Persiltech.Membership.Responses;

/// <summary>
/// Rol tal como lo devuelve la API.
/// </summary>
/// <param name="Id">Identificador que asignó ASP.NET Core Identity.</param>
/// <param name="Name">Nombre del rol.</param>
public sealed record RoleResponse(string Id, string Name);
