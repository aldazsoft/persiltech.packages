namespace Persiltech.Membership.Notifications;

/// <summary>
/// Aviso de confirmación de un cambio de teléfono.
/// </summary>
/// <param name="UserId">Identificador de la cuenta.</param>
/// <param name="PhoneNumber">Teléfono nuevo, que es el destinatario del aviso.</param>
/// <param name="FirstName">Nombre del usuario, para personalizar el saludo.</param>
/// <param name="LastName">Apellido del usuario, para personalizar el saludo.</param>
/// <param name="Token">Código que genera ASP.NET Core Identity.</param>
public sealed record PhoneChangeMessage(
    string UserId,
    string PhoneNumber,
    string FirstName,
    string LastName,
    string Token);

