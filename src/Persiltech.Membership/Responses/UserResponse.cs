namespace Persiltech.Membership.Responses;

/// <summary>
/// Usuario tal como lo devuelve la API.
/// </summary>
/// <param name="Id">Identificador que asignó ASP.NET Core Identity.</param>
/// <param name="Email">Correo de la cuenta, que es a la vez su nombre de usuario.</param>
/// <param name="FirstName">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="EmailConfirmed">Indica si el correo está confirmado.</param>
/// <param name="IsActive">
/// Indica si la cuenta está activa. Es <see langword="false"/> cuando está bloqueada.
/// </param>
/// <param name="Roles">Roles asignados al usuario.</param>
/// <remarks>
/// No expone el hash de la contraseña ni ningún otro dato de seguridad de la cuenta.
/// </remarks>
public sealed record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles);
