namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla propia, para lo que las demás no cubren.
/// </summary>
public static class MustExtension
{
    /// <summary>
    /// Exige que la entidad completa satisfaga el predicado.
    /// </summary>
    /// <remarks>
    /// El error se atribuye a la propiedad sobre la que se declaró la regla. El mensaje es
    /// obligatorio: no hay predeterminado que aplicar a una regla propia.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="predicate">Predicado que la entidad debe satisfacer.</param>
    /// <param name="errorMessage">Mensaje del error si el predicado no se cumple.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> Must<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Func<T, bool> predicate, string errorMessage) =>
        tree.Add(new Specification<T>(entity => predicate(entity)
            ? []
            : [new SpecificationError(tree.PropertyName, errorMessage)]));

    /// <summary>
    /// Exige que el valor de la propiedad satisfaga el predicado.
    /// </summary>
    /// <remarks>
    /// El predicado solo se evalúa si el valor es del tipo esperado, así que un
    /// <see langword="null"/> en un tipo por referencia no produce error. Encadena
    /// <c>IsRequired</c> si la propiedad también es obligatoria.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="predicate">Predicado que el valor debe satisfacer.</param>
    /// <param name="errorMessage">Mensaje del error si el predicado no se cumple.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> Must<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Func<TProperty, bool> predicate, string errorMessage) =>
        tree.AddRule((errors, value) =>
        {
            if (value is TProperty propertyValue && !predicate(propertyValue))
                errors.Add(new SpecificationError(tree.PropertyName, errorMessage));
        });

    /// <summary>
    /// Exige que la entidad completa satisfaga un predicado que no se resuelve en memoria,
    /// como una comprobación de unicidad contra la base de datos.
    /// </summary>
    /// <remarks>
    /// El error se atribuye a la propiedad sobre la que se declaró la regla. Combínalo con
    /// <see cref="IDomainSpecification{T}.EvaluateOnlyIfNoPreviousErrors"/> para que el
    /// trabajo caro no se ejecute mientras el dato siga siendo inválido.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="predicate">Predicado que la entidad debe satisfacer.</param>
    /// <param name="errorMessage">Mensaje del error si el predicado no se cumple.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, TProperty> MustAsync<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Func<T, CancellationToken, ValueTask<bool>> predicate, string errorMessage) =>
        tree.Add(new AsyncSpecification<T>(async (entity, cancellationToken) =>
            await predicate(entity, cancellationToken).ConfigureAwait(false)
                ? []
                : [new SpecificationError(tree.PropertyName, errorMessage)]));
}
