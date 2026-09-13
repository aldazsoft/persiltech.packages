namespace Persiltech.Membership.Internal;

/// <summary>
/// Lee de la petición qué aplicación cliente la hizo.
/// </summary>
internal static class ClientKeyReader
{
    /// <summary>
    /// Devuelve el valor de la cabecera <see cref="MembershipHeaders.ClientId"/>.
    /// </summary>
    /// <remarks>
    /// Si no viene, viene vacía o trae varios valores, devuelve <see langword="null"/>: quien
    /// redacte el correo usará su dirección de por defecto. Varios valores se descartan en lugar
    /// de tomar el primero, porque una cabecera repetida es lo que enviaría alguien tanteando,
    /// no un frontal legítimo.
    /// </remarks>
    /// <param name="httpContext">Contexto de la petición.</param>
    /// <returns>La clave del cliente, o <see langword="null"/> si no se pudo determinar.</returns>
    internal static string? ReadClientKey(this HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(MembershipHeaders.ClientId, out var values) ||
            values.Count != 1)
        {
            return null;
        }

        var value = values[0];

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
