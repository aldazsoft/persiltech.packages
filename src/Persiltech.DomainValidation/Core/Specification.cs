namespace Persiltech.DomainValidation.Core;

/// <summary>
/// Regla síncrona construida a partir de una función que devuelve los errores de evaluar una
/// entidad. Es la pieza con la que las reglas fluidas se añaden al árbol de una propiedad.
/// </summary>
/// <typeparam name="T">Tipo de la entidad que se evalúa.</typeparam>
/// <param name="validationRule">
/// Función que evalúa la entidad y devuelve los errores encontrados, o una colección vacía si
/// la satisface.
/// </param>
public class Specification<T>(
    Func<T, IEnumerable<SpecificationError>> validationRule) : ISpecification<T>
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SpecificationError>> EvaluateAsync(
        T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = validationRule(entity);

        return ValueTask.FromResult<IReadOnlyList<SpecificationError>>(
            errors as IReadOnlyList<SpecificationError> ?? [.. errors ?? []]);
    }
}
