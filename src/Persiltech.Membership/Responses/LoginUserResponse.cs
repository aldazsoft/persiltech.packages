namespace Persiltech.Membership.Responses;

/// <summary>
/// Respuesta de una autenticación correcta.
/// </summary>
/// <param name="AccessToken">
/// El JSON Web Token recién emitido. Viaja en el JSON como <c>accessToken</c>.
/// </param>
/// <param name="RefreshToken">
/// El testigo con el que se renovará la sesión. Viaja en el JSON como <c>refreshToken</c>.
/// </param>
/// <remarks>
/// Es también la respuesta de una renovación correcta: devolver solo el token de acceso
/// dejaría al cliente con un testigo ya consumido.
/// </remarks>
public sealed record LoginUserResponse(string AccessToken, string RefreshToken);
