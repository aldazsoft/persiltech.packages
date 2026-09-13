namespace Persiltech.Membership;

/// <summary>
/// Aporta reclamaciones propias del consumidor al token de acceso.
/// </summary>
/// <remarks>
/// El paquete emite un token con la identidad y los roles, que es lo único que conoce. Una
/// aplicación multiinquilino necesita además saber <em>de quién</em> es la sesión, y ese dato no
/// puede vivir aquí: la membresía no sabe qué es un inquilino. Este puerto es la costura.
/// <para>
/// Lo implementa y lo registra el consumidor, y puede haber varios. Se resuelven por petición,
/// así que pueden depender de servicios con ámbito —un contexto de base de datos, por ejemplo—.
/// </para>
/// <para>
/// Meter el dato en el token evita una consulta por petición para averiguar lo mismo, pero tiene
/// su precio: <b>viaja congelado hasta que el token caduque</b>. Si lo que se aporta puede
/// revocarse —el acceso de una persona a un inquilino, sin ir más lejos—, el consumidor tiene
/// que seguir comprobándolo en el servidor. El token dice a qué inquilino pertenecía la sesión,
/// no que siga teniendo permiso.
/// </para>
/// </remarks>
public interface IAccessTokenClaimsProvider
{
    /// <summary>
    /// Devuelve las reclamaciones que se añaden al token de esta persona.
    /// </summary>
    /// <param name="user">Usuario ya autenticado.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>
    /// Las reclamaciones a añadir. Un diccionario vacío es una respuesta válida: significa que
    /// esta persona no aporta ninguna.
    /// </returns>
    Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);
}
