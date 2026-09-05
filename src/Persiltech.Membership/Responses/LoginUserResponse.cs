namespace Persiltech.Membership.Responses;

/// <summary>
/// Respuesta de una autenticación correcta.
/// </summary>
/// <param name="AccessToken">
/// El JSON Web Token recién emitido. Viaja en el JSON como <c>accessToken</c>.
/// </param>
public sealed record LoginUserResponse(string AccessToken);
