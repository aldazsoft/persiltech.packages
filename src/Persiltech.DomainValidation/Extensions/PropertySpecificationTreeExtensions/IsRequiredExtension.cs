namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige que la propiedad traiga valor.
/// </summary>
public static class IsRequiredExtension
{
    /// <summary>
    /// Exige que la propiedad traiga valor: rechaza <see langword="null"/> y, en una cadena,
    /// el texto vacío o formado solo por espacios en blanco.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> IsRequired<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? ErrorMessages.IsRequired));
            }
        });
}
