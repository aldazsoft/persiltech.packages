namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Reglas de igualdad, contra un valor fijo o contra otra propiedad de la entidad.
/// </summary>
public static class EqualExtension
{
    /// <summary>
    /// Exige que la propiedad sea igual al valor indicado.
    /// </summary>
    /// <remarks>
    /// Dos valores <see langword="null"/> se consideran iguales; si solo uno lo es, la regla
    /// falla.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonValue">Valor con el que se compara.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> Equal<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        TProperty comparisonValue,
        string? errorMessage = null) where TProperty : IComparable<TProperty> =>
        AddRule(tree, entity => comparisonValue, errorMessage);

    /// <summary>
    /// Exige que la propiedad sea igual a otra propiedad de la misma entidad, como en una
    /// confirmación de contraseña.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonProperty">Expresión que selecciona la propiedad con la que se compara.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> Equal<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Expression<Func<T, TProperty>> comparisonProperty,
        string? errorMessage = null) where TProperty : IComparable<TProperty> =>
        // La expresión se compila al declarar la regla, no en cada validación.
        AddRule(tree, comparisonProperty.Compile(), errorMessage);

    /// <summary>
    /// Exige que la propiedad anulable por valor sea igual al valor indicado.
    /// </summary>
    /// <remarks>
    /// Dos propiedades sin valor se consideran iguales; si solo una lo está, la regla falla.
    /// Esta sobrecarga existe porque la restricción <see cref="IComparable{T}"/> no admite
    /// tipos anulables por valor.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo subyacente de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonValue">Valor con el que se compara.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty?> Equal<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty?> tree,
        TProperty? comparisonValue,
        string? errorMessage = null) where TProperty : struct, IComparable<TProperty> =>
        AddNullableRule(tree, entity => comparisonValue, errorMessage);

    /// <summary>
    /// Exige que la propiedad anulable por valor sea igual a otra propiedad de la misma
    /// entidad.
    /// </summary>
    /// <remarks>
    /// Dos propiedades sin valor se consideran iguales; si solo una lo está, la regla falla.
    /// Esta sobrecarga existe porque la restricción <see cref="IComparable{T}"/> no admite
    /// tipos anulables por valor.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo subyacente de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="comparisonProperty">Expresión que selecciona la propiedad con la que se compara.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty?> Equal<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty?> tree,
        Expression<Func<T, TProperty?>> comparisonProperty,
        string? errorMessage = null) where TProperty : struct, IComparable<TProperty> =>
        AddNullableRule(tree, comparisonProperty.Compile(), errorMessage);

    static PropertySpecificationsTree<T, TProperty> AddRule<T, TProperty>(
        PropertySpecificationsTree<T, TProperty> tree,
        Func<T, TProperty> getComparisonValue,
        string? errorMessage) where TProperty : IComparable<TProperty> =>
        tree.AddRule((errors, value, entity) =>
        {
            TProperty comparisonValue = getComparisonValue(entity);

            bool areEqual = value is null
                ? comparisonValue is null
                : comparisonValue is not null && value.CompareTo(comparisonValue) == 0;

            if (!areEqual)
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? ErrorMessages.Equal));
            }
        });

    static PropertySpecificationsTree<T, TProperty?> AddNullableRule<T, TProperty>(
        PropertySpecificationsTree<T, TProperty?> tree,
        Func<T, TProperty?> getComparisonValue,
        string? errorMessage) where TProperty : struct, IComparable<TProperty> =>
        tree.AddRule((errors, value, entity) =>
        {
            TProperty? comparisonValue = getComparisonValue(entity);

            if (!Nullable.Equals(value, comparisonValue))
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? ErrorMessages.Equal));
            }
        });
}
