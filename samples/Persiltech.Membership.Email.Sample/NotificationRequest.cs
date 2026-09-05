namespace Persiltech.Membership.Email.Sample;

/// <summary>
/// Datos con los que el sample dispara un aviso. En una aplicación real los aporta
/// Persiltech.Membership al llamar al puerto; aquí llegan por la petición para poder
/// verificar el correo que sale.
/// </summary>
public sealed record NotificationRequest(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string Token);
