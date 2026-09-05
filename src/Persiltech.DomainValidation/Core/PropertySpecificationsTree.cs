namespace Persiltech.DomainValidation.Core;

/// <summary>
/// Árbol de reglas declaradas sobre una propiedad. Lo devuelve
/// <see cref="DomainSpecificationBase{T}.Property{TProperty}"/>, y las reglas fluidas lo
/// devuelven a su vez para poder encadenarlas.
/// </summary>
/// <typeparam name="T">Tipo de la entidad a la que pertenece la propiedad.</typeparam>
/// <typeparam name="TProperty">Tipo de la propiedad.</typeparam>
/// <param name="propertyExpression">Expresión que selecciona la propiedad en la entidad.</param>
/// <param name="stopOnFirstPropertySpecificationError">
/// Indica si la evaluación debe detenerse en cuanto una de sus reglas falle.
/// </param>
/// <exception cref="ArgumentException">
/// Si la expresión no selecciona un miembro de la entidad, porque entonces los errores no
/// tendrían a qué propiedad atribuirse.
/// </exception>
public class PropertySpecificationsTree<T, TProperty>(
    Expression<Func<T, TProperty>> propertyExpression,
    bool stopOnFirstPropertySpecificationError = false) : IPropertySpecificationsTree<T>
{
    readonly Func<T, TProperty> PropertyExpressionDelegate =
        propertyExpression.Compile();

    readonly List<ISpecification<T>> DeclaredSpecifications = [];

    /// <inheritdoc />
    public string PropertyName { get; } =
        propertyExpression.GetPropertyName() ??
        throw new ArgumentException(
            $"La expresión debe seleccionar un miembro de {typeof(T).Name} " +
            $"(Ej. entity => entity.Code). '{propertyExpression}' no lo hace, " +
            "así que los errores no tendrían a qué propiedad atribuirse.",
            nameof(propertyExpression));

    /// <inheritdoc />
    public IReadOnlyList<ISpecification<T>> Specifications => DeclaredSpecifications;

    /// <inheritdoc />
    public bool StopOnFirstPropertySpecificationError =>
        stopOnFirstPropertySpecificationError;

    /// <summary>
    /// Obtiene el valor de la propiedad en la entidad indicada.
    /// </summary>
    /// <param name="entity">Entidad de la que se lee la propiedad.</param>
    /// <returns>El valor de la propiedad.</returns>
    public TProperty GetPropertyValue(T entity) =>
        PropertyExpressionDelegate(entity);

    /// <summary>
    /// Añade una regla al árbol. Es el punto por el que entran las reglas fluidas, y también
    /// por el que se declara una regla propia que no encaje en ninguna de ellas.
    /// </summary>
    /// <param name="specification">Regla que se añade.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public PropertySpecificationsTree<T, TProperty> Add(
        ISpecification<T> specification)
    {
        DeclaredSpecifications.Add(specification);

        return this;
    }
}
