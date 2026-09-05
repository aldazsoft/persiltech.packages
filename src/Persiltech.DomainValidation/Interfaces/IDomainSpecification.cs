namespace Persiltech.DomainValidation.Interfaces;

/// <summary>
/// Especificación de dominio: el conjunto de reglas de negocio que una entidad debe cumplir.
/// </summary>
/// <remarks>
/// La validación no deja estado en la especificación: devuelve sus errores, de modo que una
/// misma instancia puede validar varias entidades a la vez sin que los resultados se pisen.
/// </remarks>
/// <typeparam name="T">Tipo de la entidad que se valida.</typeparam>
public interface IDomainSpecification<T>
{
    /// <summary>
    /// Indica si la especificación es condicional, es decir, si solo debe evaluarse cuando
    /// ninguna especificación anterior produjo errores. Sirve para posponer el trabajo caro
    /// —una consulta a la base de datos, por ejemplo— mientras el dato siga siendo inválido.
    /// </summary>
    bool EvaluateOnlyIfNoPreviousErrors { get; }

    /// <summary>
    /// Indica si la evaluación debe detenerse en cuanto una propiedad produzca errores, en
    /// lugar de recorrer las demás propiedades de la entidad.
    /// </summary>
    bool StopOnFirstEntitySpecificationError { get; }

    /// <summary>
    /// Valida la entidad contra las reglas de la especificación.
    /// </summary>
    /// <param name="entity">Entidad que se valida.</param>
    /// <param name="cancellationToken">Token que cancela la validación.</param>
    /// <returns>
    /// Los errores encontrados, o una colección vacía si la entidad cumple todas las reglas.
    /// </returns>
    Task<IReadOnlyList<SpecificationError>> ValidateAsync(
        T entity, CancellationToken cancellationToken = default);
}
