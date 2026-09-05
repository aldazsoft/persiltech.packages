namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige un valor mayor o igual que otro.
/// </summary>
public static class GreaterThanOrEqualToExtension
{
    /// <summary>
    /// Exige que la propiedad sea mayor o igual que el valor indicado.
    /// </summary>
    /// <remarks>
    /// Un valor <see langword="null"/> se rechaza: no hay nada que pueda alcanzar al valor de
    /// comparación. Si la propiedad también es obligatoria y prefieres un solo error, encadena
    /// <c>IsRequired</c> y abre el árbol con <c>stopOnFirstPropertySpecificationError: true</c>.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonValue">Valor mínimo admitido.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty>
        GreaterThanOrEqualTo<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        TProperty comparisonValue,
        string? errorMessage = null) where TProperty : IComparable<TProperty> =>
        tree.AddRule((errors, value) =>
        {
            if (value is null || value.CompareTo(comparisonValue) < 0)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? string.Format(
                        ErrorMessages.GreaterThanOrEqualTo, comparisonValue)));
            }
        });

    /// <summary>
    /// Exige que la propiedad anulable por valor sea mayor o igual que el valor indicado.
    /// </summary>
    /// <remarks>
    /// Una propiedad sin valor se rechaza. Esta sobrecarga existe porque la restricción
    /// <see cref="IComparable{T}"/> no admite tipos anulables por valor, de modo que la
    /// sobrecarga general no se puede declarar sobre <c>int?</c>, <c>decimal?</c> ni
    /// similares.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo subyacente de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonValue">Valor mínimo admitido.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty?>
        GreaterThanOrEqualTo<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty?> tree,
        TProperty comparisonValue,
        string? errorMessage = null) where TProperty : struct, IComparable<TProperty> =>
        tree.AddRule((errors, value) =>
        {
            if (value is not TProperty propertyValue ||
                propertyValue.CompareTo(comparisonValue) < 0)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? string.Format(
                        ErrorMessages.GreaterThanOrEqualTo, comparisonValue)));
            }
        });
}
