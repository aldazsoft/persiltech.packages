namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Dónde viven los testigos de la sesión entre peticiones.
/// </summary>
/// <remarks>
/// Es una interfaz porque dónde se guarda un testigo es una decisión de seguridad del
/// consumidor, no del paquete. La implementación por defecto usa <c>localStorage</c>, que es
/// lo único que funciona sin un backend propio, pero sobrevive al cierre de la pestaña y lo
/// lee cualquier script de la página: un XSS lo expone. Quien tenga backend puede guardar el
/// testigo de renovación en una cookie <c>HttpOnly</c> e implementar esto a su manera.
/// </remarks>
public interface IMembershipTokenStore
{
    /// <summary>
    /// Testigos de la sesión actual.
    /// </summary>
    /// <returns>El par guardado, o <see langword="null"/> si no hay sesión.</returns>
    ValueTask<MembershipTokens?> GetAsync();

    /// <summary>
    /// Guarda el par recién emitido, sustituyendo al anterior.
    /// </summary>
    /// <param name="tokens">Testigos que devolvió la API.</param>
    /// <returns>La tarea que representa el guardado.</returns>
    ValueTask SetAsync(MembershipTokens tokens);

    /// <summary>
    /// Borra la sesión guardada.
    /// </summary>
    /// <returns>La tarea que representa el borrado.</returns>
    ValueTask ClearAsync();
}
