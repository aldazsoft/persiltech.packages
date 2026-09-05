namespace Persiltech.Membership.Notifications;

/// <summary>
/// Aviso de reinicio de una contraseña olvidada.
/// </summary>
/// <param name="UserId">Identificador de la cuenta.</param>
/// <param name="Email">Correo al que va dirigido el aviso.</param>
/// <param name="FirstName">Nombre del usuario, para personalizar el saludo.</param>
/// <param name="LastName">Apellido del usuario, para personalizar el saludo.</param>
/// <param name="Token">Testigo de reinicio que genera ASP.NET Core Identity.</param>
public sealed record PasswordResetMessage(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string Token);

