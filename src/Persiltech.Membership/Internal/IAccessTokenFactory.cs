namespace Persiltech.Membership.Internal;

/// <summary>
/// Emisor del token de acceso. Aísla la emisión del manejador HTTP.
/// </summary>
internal interface IAccessTokenFactory
{
    /// <summary>
    /// Emite un token de acceso para el usuario indicado.
    /// </summary>
    /// <remarks>
    /// Es asíncrono porque las aportaciones de
    /// <see cref="IAccessTokenClaimsProvider"/> pueden salir de una consulta.
    /// </remarks>
    /// <param name="user">Usuario ya autenticado.</param>
    /// <param name="roles">
    /// Roles del usuario, que viajan como reclamaciones <see cref="ClaimTypes.Role"/>. Los
    /// recibe en lugar de leerlos por su cuenta para no depender de
    /// <see cref="UserManager{TUser}"/> y seguir siendo un servicio sin estado.
    /// </param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>El token serializado y firmado.</returns>
    Task<string> CreateAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);
}
