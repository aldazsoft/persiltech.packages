namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige una longitud exacta en una cadena.
/// </summary>
public static class HasFixedLengthExtension
{
    /// <summary>
    /// Exige que la cadena tenga exactamente la longitud indicada. Un valor
    /// <see langword="null"/> se rechaza.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="length">Longitud exacta exigida.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, string> HasFixedLength<T>(
        this PropertySpecificationsTree<T, string> tree,
        int length, string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (value == null || value.Length != length)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ??
                    string.Format(ErrorMessages.HasFixedLength, length)));
            }
        });
}
