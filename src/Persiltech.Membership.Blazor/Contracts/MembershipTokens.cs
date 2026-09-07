namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Par de testigos de una sesión, tal como los devuelve la API.
/// </summary>
/// <param name="AccessToken">JSON Web Token con el que se firma cada petición.</param>
/// <param name="RefreshToken">Testigo con el que se renueva la sesión.</param>
public sealed record MembershipTokens(string AccessToken, string RefreshToken);
