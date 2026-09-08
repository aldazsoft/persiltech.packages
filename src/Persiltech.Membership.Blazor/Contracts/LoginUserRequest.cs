namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Cuerpo de la petición de autenticación.
/// </summary>
/// <param name="Email">Correo con el que se registró la cuenta.</param>
/// <param name="Password">Contraseña de la cuenta.</param>
/// <param name="TwoFactorCode">
/// Segundo factor, solo si la cuenta lo tiene activado.
/// </param>
public sealed record LoginUserRequest(string Email, string Password, string? TwoFactorCode = null);
