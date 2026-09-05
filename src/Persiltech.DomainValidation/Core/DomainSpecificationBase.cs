namespace Persiltech.DomainValidation.Core;

/// <summary>
/// Base de toda especificación de dominio. Las clases derivadas declaran sus reglas en el
/// constructor, con <see cref="Property{TProperty}"/>, y sobrescriben
/// <see cref="ValidateSpecificationsAsync(T, CancellationToken)"/> cuando necesitan
/// comprobaciones que no se resuelven en memoria.
/// </summary>
/// <typeparam name="T">Tipo de la entidad que se valida.</typeparam>
/// <param name="evaluateOnlyIfNoPreviousErrors">
/// Marca la especificación como condicional: el validador solo la evaluará si ninguna
/// especificación anterior produjo errores.
/// </param>
/// <param name="stopOnFirstEntitySpecificationError">
/// Detiene la validación en cuanto una propiedad produzca errores, en lugar de recorrer las
/// demás propiedades de la entidad.
/// </param>
public abstract class DomainSpecificationBase<T>(
    bool evaluateOnlyIfNoPreviousErrors = false,
    bool stopOnFirstEntitySpecificationError = false) : IDomainSpecification<T>
{
    /// <inheritdoc />
    public bool EvaluateOnlyIfNoPreviousErrors { get; } = evaluateOnlyIfNoPreviousErrors;

    /// <inheritdoc />
    public bool StopOnFirstEntitySpecificationError { get; } =
        stopOnFirstEntitySpecificationError;

    readonly List<IPropertySpecificationsTree<T>> PropertySpecificationsForest = [];

    /// <summary>
    /// Abre un árbol de reglas sobre una propiedad de la entidad.
    /// </summary>
    /// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
    /// <param name="propertyExpression">Expresión que selecciona la propiedad.</param>
    /// <param name="stopOnFirstPropertySpecificationError">
    /// Detiene las reglas de esa propiedad en cuanto una falle, para no acumular errores que
    /// se explican entre sí.
    /// </param>
    /// <returns>
    /// El árbol de la propiedad, sobre el que se encadenan las reglas fluidas.
    /// </returns>
    protected PropertySpecificationsTree<T, TProperty> Property<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        bool stopOnFirstPropertySpecificationError = false)
    {
        var tree = new PropertySpecificationsTree<T, TProperty>(
            propertyExpression, stopOnFirstPropertySpecificationError);

        PropertySpecificationsForest.Add(tree);

        return tree;
    }

    async Task<List<SpecificationError>> ValidatePropertySpecificationsForestAsync(
        T entity, CancellationToken cancellationToken)
    {
        List<SpecificationError> specificationErrors = [];

        foreach (var tree in PropertySpecificationsForest)
        {
            foreach (var specification in tree.Specifications)
            {
                var errors = await specification
                    .EvaluateAsync(entity, cancellationToken).ConfigureAwait(false);

                if (errors.Count != 0)
                {
                    specificationErrors.AddRange(errors);

                    if (tree.StopOnFirstPropertySpecificationError)
                        break;
                }
            }

            if (specificationErrors.Count != 0 && StopOnFirstEntitySpecificationError)
                break;
        }

        return specificationErrors;
    }

    /// <summary>
    /// Punto de extensión para las comprobaciones que no se pueden declarar como reglas sobre
    /// una propiedad, típicamente porque necesitan trabajo asíncrono: consultar la base de
    /// datos, llamar a un servicio.
    /// </summary>
    /// <remarks>
    /// La implementación predeterminada no produce errores. Combínalo con
    /// <see cref="IDomainSpecification{T}.EvaluateOnlyIfNoPreviousErrors"/> para que el trabajo
    /// caro no se ejecute mientras el dato siga siendo inválido.
    /// </remarks>
    /// <param name="entity">Entidad que se valida.</param>
    /// <param name="cancellationToken">Token que cancela la validación.</param>
    /// <returns>Los errores encontrados, o una lista vacía.</returns>
    protected virtual Task<List<SpecificationError>> ValidateSpecificationsAsync(
        T entity, CancellationToken cancellationToken = default) =>
        Task.FromResult<List<SpecificationError>>([]);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpecificationError>> ValidateAsync(
        T entity, CancellationToken cancellationToken = default) =>
        [
            .. await ValidatePropertySpecificationsForestAsync(
                entity, cancellationToken).ConfigureAwait(false),
            .. await ValidateSpecificationsAsync(
                entity, cancellationToken).ConfigureAwait(false) ?? []
        ];
}
