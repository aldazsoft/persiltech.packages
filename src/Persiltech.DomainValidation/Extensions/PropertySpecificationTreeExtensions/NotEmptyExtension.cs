namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige que la colección traiga al menos un elemento.
/// </summary>
public static class NotEmptyExtension
{
    /// <summary>
    /// Exige que la propiedad no esté vacía: rechaza <see langword="null"/> y la colección sin
    /// elementos.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> NotEmpty<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (value is null ||
                value is IEnumerable collection && !collection.Cast<object>().Any())
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? ErrorMessages.NotEmpty));
            }
        });
}
