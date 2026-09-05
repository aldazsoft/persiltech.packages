namespace Persiltech.DomainValidation.Interfaces;

/// <summary>
/// Regla que una entidad cumple o no cumple.
/// </summary>
/// <remarks>
/// La evaluación no deja estado en la regla: devuelve sus errores, de modo que una misma
/// instancia puede evaluar varias entidades a la vez sin que los resultados se pisen.
/// </remarks>
/// <typeparam name="T">Tipo de la entidad que se evalúa.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Evalúa la entidad.
    /// </summary>
    /// <param name="entity">Entidad que se evalúa.</param>
    /// <param name="cancellationToken">Token que cancela la evaluación.</param>
    /// <returns>
    /// Los errores encontrados, o una colección vacía si la entidad satisface la regla.
    /// </returns>
    ValueTask<IReadOnlyList<SpecificationError>> EvaluateAsync(
        T entity, CancellationToken cancellationToken = default);
}
