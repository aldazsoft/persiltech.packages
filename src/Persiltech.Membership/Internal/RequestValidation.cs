namespace Persiltech.Membership.Internal;

/// <summary>
/// Validación de los cuerpos de petición con anotaciones de datos.
/// </summary>
internal static class RequestValidation
{
    /// <summary>
    /// Valida la petición y, si no es válida, entrega sus errores agrupados por campo.
    /// </summary>
    /// <param name="request">Cuerpo de la petición ya deserializado.</param>
    /// <param name="errors">
    /// Diccionario con las claves en camelCase, o <see langword="null"/> si la petición es
    /// válida. Los errores que no corresponden a un campo concreto usan la clave vacía.
    /// </param>
    /// <returns><see langword="true"/> si la petición es válida.</returns>
    internal static bool TryValidate(
        object request,
        [NotNullWhen(false)] out Dictionary<string, string[]>? errors)
    {
        List<ValidationResult> results = [];

        if (Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            errors = null;
            return true;
        }

        Dictionary<string, List<string>> messages = [];

        foreach (var result in results)
        {
            IEnumerable<string> keys = result.MemberNames.Any()
                ? result.MemberNames.Select(ToCamelCase)
                : [string.Empty];

            foreach (var key in keys)
            {
                if (!messages.TryGetValue(key, out var accumulated))
                {
                    accumulated = [];
                    messages[key] = accumulated;
                }

                accumulated.Add(result.ErrorMessage ?? string.Empty);
            }
        }

        errors = messages.ToDictionary(m => m.Key, m => m.Value.ToArray());
        return false;
    }

    private static string ToCamelCase(string memberName) =>
        string.IsNullOrEmpty(memberName)
            ? memberName
            : char.ToLowerInvariant(memberName[0]) + memberName[1..];
}
