namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Cuerpo de la petición que fija una contraseña nueva.
/// </summary>
/// <param name="Email">Correo de la cuenta.</param>
/// <param name="Token">Testigo que llegó por correo.</param>
/// <param name="NewPassword">Contraseña nueva.</param>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
