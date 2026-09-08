namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Usuario tal como lo devuelve la API.
/// </summary>
/// <param name="Id">Identificador que asigna Identity.</param>
/// <param name="Email">Correo de la cuenta.</param>
/// <param name="FirstName">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="EmailConfirmed">Si el correo está confirmado.</param>
/// <param name="IsActive">Si la cuenta está activa.</param>
/// <param name="Roles">Roles asignados a la cuenta.</param>
public sealed record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles);
