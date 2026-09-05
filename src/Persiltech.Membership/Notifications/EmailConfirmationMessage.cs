namespace Persiltech.Membership.Notifications;

/// <summary>
/// Aviso de confirmación del correo con el que se registró una cuenta.
/// </summary>
/// <param name="UserId">Identificador de la cuenta.</param>
/// <param name="Email">Correo al que va dirigido el aviso.</param>
/// <param name="FirstName">Nombre del usuario, para personalizar el saludo.</param>
/// <param name="LastName">Apellido del usuario, para personalizar el saludo.</param>
/// <param name="Token">
/// Testigo que genera ASP.NET Core Identity. Es lo que hay que hacer llegar al usuario para
/// que la confirmación pueda completarse.
/// </param>
public sealed record EmailConfirmationMessage(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string Token);

