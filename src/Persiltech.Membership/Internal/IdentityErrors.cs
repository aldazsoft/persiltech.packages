namespace Persiltech.Membership.Internal;

/// <summary>
/// Traduce el resultado de una operación de ASP.NET Core Identity al diccionario de
/// errores que espera <c>ValidationProblemDetails</c>.
/// </summary>
internal static class IdentityErrors
{
    /// <summary>
    /// Agrupa los errores del resultado bajo una única clave.
    /// </summary>
    /// <param name="result">Resultado fallido de Identity.</param>
    /// <param name="memberName">
    /// Nombre del miembro al que se atribuyen los errores. Se convierte a camelCase para
    /// que la clave coincida con el campo del JSON.
    /// </param>
    /// <returns>Diccionario con una sola entrada.</returns>
    internal static Dictionary<string, string[]> ToErrors(IdentityResult result, string memberName)
    {
        var key = char.ToLowerInvariant(memberName[0]) + memberName[1..];

        return new Dictionary<string, string[]>
        {
            [key] = [.. result.Errors.Select(e => e.Description)]
        };
    }

    /// <summary>
    /// Agrupa los errores del resultado bajo la clave de nivel de formulario.
    /// </summary>
    /// <param name="result">Resultado fallido de Identity.</param>
    /// <returns>Diccionario con una sola entrada, la de clave vacía.</returns>
    internal static Dictionary<string, string[]> ToErrors(IdentityResult result) =>
        new() { [string.Empty] = [.. result.Errors.Select(e => e.Description)] };
}
