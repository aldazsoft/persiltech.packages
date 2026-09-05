namespace Persiltech.Membership.OAuth;

/// <summary>
/// Aplicación cliente que el servidor de autorización reconoce.
/// </summary>
/// <param name="ClientId">Identificador público del cliente.</param>
/// <param name="DisplayName">Nombre legible, para pantallas de consentimiento y registros.</param>
/// <param name="RedirectUris">
/// URIs de vuelta admitidas. Se comparan de forma exacta: una URI que no esté aquí se
/// rechaza, que es lo que impide que un tercero se lleve el código de autorización.
/// </param>
/// <param name="Scopes">Ámbitos que el cliente puede pedir.</param>
/// <param name="ClientSecret">
/// Secreto del cliente, o <see langword="null"/> para un cliente público. Una aplicación de
/// navegador o móvil <em>no puede</em> guardar un secreto, y por eso usa PKCE en su lugar.
/// </param>
public sealed record MembershipOAuthClient(
    string ClientId,
    string DisplayName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> Scopes,
    string? ClientSecret = null);
