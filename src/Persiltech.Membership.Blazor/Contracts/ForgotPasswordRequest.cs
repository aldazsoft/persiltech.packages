namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Cuerpo de la petición de reinicio de una contraseña olvidada.
/// </summary>
/// <param name="Email">Correo de la cuenta.</param>
public sealed record ForgotPasswordRequest(string Email);
