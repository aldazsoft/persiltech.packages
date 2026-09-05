namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige una longitud mínima en una cadena.
/// </summary>
public static class HasMinLengthExtension
{
    /// <summary>
    /// Exige que la cadena tenga al menos la longitud indicada. Un valor
    /// <see langword="null"/> se rechaza.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="minLength">Longitud mínima exigida.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, string> HasMinLength<T>(
        this PropertySpecificationsTree<T, string> tree,
        int minLength, string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (value == null || value.Length < minLength)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ??
                    string.Format(ErrorMessages.HasMinLength, minLength)));
            }
        });
}
