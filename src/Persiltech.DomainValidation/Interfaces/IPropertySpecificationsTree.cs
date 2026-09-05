namespace Persiltech.DomainValidation.Interfaces;

/// <summary>
/// Conjunto de reglas declaradas sobre una misma propiedad de la entidad.
/// </summary>
/// <typeparam name="T">Tipo de la entidad a la que pertenece la propiedad.</typeparam>
public interface IPropertySpecificationsTree<T>
{
    /// <summary>
    /// Nombre de la propiedad, tal como aparecerá en los errores que produzcan sus reglas.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Reglas declaradas sobre la propiedad, en el orden en que se declararon.
    /// </summary>
    IReadOnlyList<ISpecification<T>> Specifications { get; }

    /// <summary>
    /// Indica si la evaluación de esta propiedad debe detenerse en cuanto una de sus reglas
    /// falle, para no acumular errores que se explican entre sí.
    /// </summary>
    bool StopOnFirstPropertySpecificationError { get; }
}
