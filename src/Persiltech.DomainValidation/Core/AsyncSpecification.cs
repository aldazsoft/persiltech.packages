namespace Persiltech.DomainValidation.Core;

/// <summary>
/// Regla asíncrona, para lo que no se resuelve en memoria: validar cada elemento de una
/// colección con su propio validador, consultar la base de datos, llamar a un servicio.
/// </summary>
/// <typeparam name="T">Tipo de la entidad que se evalúa.</typeparam>
/// <param name="validationRule">
/// Función que evalúa la entidad y devuelve los errores encontrados, o una colección vacía si
/// la satisface.
/// </param>
public class AsyncSpecification<T>(
    Func<T, CancellationToken, ValueTask<IEnumerable<SpecificationError>>> validationRule)
    : ISpecification<T>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<SpecificationError>> EvaluateAsync(
        T entity, CancellationToken cancellationToken = default)
    {
        var errors = await validationRule(entity, cancellationToken)
            .ConfigureAwait(false);

        return errors as IReadOnlyList<SpecificationError> ?? [.. errors ?? []];
    }
}
