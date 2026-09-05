namespace Persiltech.Membership;

/// <summary>
/// Cuenta de administración con la que arranca una instalación nueva.
/// </summary>
/// <param name="Email">Correo de la cuenta, que es a la vez su nombre de usuario.</param>
/// <param name="Password">
/// Contraseña inicial. Es un secreto: el consumidor la aporta desde su configuración, y
/// conviene cambiarla en el primer acceso.
/// </param>
/// <param name="FirstName">Nombre del administrador.</param>
/// <param name="LastName">Apellido del administrador.</param>
/// <param name="RoleName">Rol que se crea y se asigna a la cuenta.</param>
public sealed record MembershipAdministrator(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string RoleName = "Administrator");
