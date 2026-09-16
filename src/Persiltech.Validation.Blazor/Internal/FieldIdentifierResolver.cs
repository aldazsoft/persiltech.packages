namespace Persiltech.Validation.Blazor.Internal;

/// <summary>
/// Traduce la clave que devuelve la API al campo del modelo al que pertenece.
/// </summary>
/// <remarks>
/// Es la pieza que hace que el error aterrice donde debe. <c>ValidationProblemDetails</c> nombra
/// los campos como los serializó —<c>email</c>, <c>newPassword</c>, <c>address.city</c>,
/// <c>items[0].name</c>—, y Blazor los identifica por el objeto que los contiene y el nombre de
/// la propiedad. Aquí se recorre la ruta para dar con ese objeto.
/// <para>
/// El emparejamiento ignora mayúsculas: el servidor manda camelCase y la propiedad es PascalCase.
/// </para>
/// </remarks>
internal static class FieldIdentifierResolver
{
    private static readonly char[] Separators = ['.', '['];

    /// <summary>
    /// Busca el campo que nombra la clave.
    /// </summary>
    /// <param name="model">Modelo del formulario.</param>
    /// <param name="key">Clave tal como llegó de la API.</param>
    /// <param name="field">El campo encontrado.</param>
    /// <returns>
    /// <see langword="false"/> si la ruta no existe en el modelo, y entonces el mensaje no
    /// pertenece a ningún campo de este formulario.
    /// </returns>
    internal static bool TryResolve(object model, string key, out FieldIdentifier field)
    {
        field = default;

        if (model is null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var owner = model;
        var path = key.Trim();

        while (true)
        {
            var separator = path.IndexOfAny(Separators);

            if (separator < 0)
            {
                // El último tramo nombra la propiedad; no se navega hacia ella, porque es
                // justo la que Blazor necesita junto a su contenedor.
                return HasProperty(owner, path, out var name) &&
                       Assign(owner, name, out field);
            }

            var token = path[..separator];
            var isIndexer = path[separator] == '[';

            path = path[(separator + 1)..];

            if (isIndexer)
            {
                if (!TryStep(owner, token, out owner))
                {
                    return false;
                }

                var close = path.IndexOf(']');

                if (close < 0)
                {
                    return false;
                }

                if (!TryIndex(owner, path[..close], out owner))
                {
                    return false;
                }

                // Tras el corchete puede venir un punto: 'items[0].name'.
                path = path[(close + 1)..].TrimStart('.');

                continue;
            }

            if (!TryStep(owner, token, out owner))
            {
                return false;
            }
        }
    }

    private static bool Assign(object owner, string propertyName, out FieldIdentifier field)
    {
        field = new FieldIdentifier(owner, propertyName);

        return true;
    }

    private static bool HasProperty(object owner, string name, out string propertyName)
    {
        var property = FindProperty(owner, name);

        propertyName = property?.Name ?? string.Empty;

        return property is not null;
    }

    private static bool TryStep(object owner, string name, out object next)
    {
        next = owner;

        var property = FindProperty(owner, name);
        var value = property?.GetValue(owner);

        if (value is null)
        {
            return false;
        }

        next = value;

        return true;
    }

    /// <summary>
    /// Entra al elemento que señala el corchete.
    /// </summary>
    /// <remarks>
    /// Un arreglo no tiene propiedad <c>Item</c> —el indexador de <c>T[]</c> lo sirve el
    /// entorno de ejecución, no un miembro que la reflexión encuentre—, así que va por
    /// <see cref="Array"/>. Lo que sí la tiene, como <c>List&lt;T&gt;</c>, entra por
    /// <see cref="IList"/>, y el indexador por reflexión queda para el resto —un
    /// <c>IReadOnlyList&lt;T&gt;</c>, o una colección de la casa con indexador propio—.
    /// </remarks>
    private static bool TryIndex(object collection, string index, out object next)
    {
        next = collection;

        try
        {
            var value = collection switch
            {
                Array array => array.GetValue(int.Parse(index, CultureInfo.InvariantCulture)),
                IList list => list[int.Parse(index, CultureInfo.InvariantCulture)],
                _ => ByIndexer(collection, index)
            };

            if (value is null)
            {
                return false;
            }

            next = value;

            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or
                         OverflowException or
                         InvalidCastException or
                         IndexOutOfRangeException or
                         ArgumentException or
                         AmbiguousMatchException or
                         TargetInvocationException)
        {
            // Un índice fuera de rango, o que no es un número, significa que la clave no
            // describe este modelo. No es motivo para tumbar el formulario.
            return false;
        }
    }

    private static object? ByIndexer(object collection, string index)
    {
        var indexer = collection.GetType().GetProperty("Item");

        if (indexer is null)
        {
            return null;
        }

        var parameterType = indexer.GetIndexParameters()[0].ParameterType;

        return indexer.GetValue(
            collection,
            [Convert.ChangeType(index, parameterType, CultureInfo.InvariantCulture)]);
    }

    /// <summary>
    /// Busca la propiedad, primero por su nombre exacto y luego sin distinguir mayúsculas.
    /// </summary>
    /// <remarks>
    /// La segunda pasada hace falta porque el servidor serializa en camelCase y la propiedad
    /// es PascalCase: una búsqueda exacta no encontraría ninguna y los errores no llegarían a
    /// ningún campo.
    /// <para>
    /// El intento exacto va primero por un motivo: ignorando mayúsculas, un tipo con dos
    /// propiedades que solo se diferencian en ellas hace que la reflexión no sepa cuál elegir
    /// y lance. Si una de las dos coincide exactamente, esa es la respuesta; si no, no hay
    /// forma de decidir y el mensaje se queda sin campo, que es mejor que una excepción en
    /// mitad de un formulario.
    /// </para>
    /// </remarks>
    private static PropertyInfo? FindProperty(object owner, string name)
    {
        var type = owner.GetType();

        try
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                ?? type.GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }
        catch (AmbiguousMatchException)
        {
            return null;
        }
    }
}
