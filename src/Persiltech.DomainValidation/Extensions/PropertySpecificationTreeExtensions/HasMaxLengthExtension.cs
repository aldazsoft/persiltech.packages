namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que limita la longitud de una cadena.
/// </summary>
public static class HasMaxLengthExtension
{
    /// <summary>
    /// Exige que la cadena no exceda la longitud indicada.
    /// </summary>
    /// <remarks>
    /// Un valor <see langword="null"/> se da por bueno: no hay longitud que exceder. Si la
    /// propiedad también es obligatoria, encadena <c>IsRequired</c>.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="maxLength">Longitud máxima admitida.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, string> HasMaxLength<T>(
        this PropertySpecificationsTree<T, string> tree,
        int maxLength, string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (value != null && value.Length > maxLength)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ??
                    string.Format(ErrorMessages.HasMaxLength, maxLength)));
            }
        });
}
