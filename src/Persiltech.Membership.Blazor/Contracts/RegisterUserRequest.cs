namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Cuerpo de la petición de registro.
/// </summary>
/// <param name="Email">Correo, que será también el nombre de usuario.</param>
/// <param name="Password">Contraseña de la cuenta.</param>
/// <param name="FirstName">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);
