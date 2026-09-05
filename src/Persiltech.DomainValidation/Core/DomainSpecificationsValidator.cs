namespace Persiltech.DomainValidation.Core;

/// <summary>
/// Validador que evalúa todas las especificaciones registradas para un tipo.
/// </summary>
/// <remarks>
/// Evalúa primero las especificaciones incondicionales y reúne sus errores. Solo si ninguna
/// produjo errores pasa a las condicionales —las que declaran
/// <see cref="IDomainSpecification{T}.EvaluateOnlyIfNoPreviousErrors"/>—, y ahí se detiene en
/// la primera que falle.
/// </remarks>
/// <typeparam name="T">Tipo de la entidad que se valida.</typeparam>
/// <param name="specifications">Especificaciones que la entidad debe cumplir.</param>
public class DomainSpecificationsValidator<T>(
    IEnumerable<IDomainSpecification<T>> specifications) : IDomainSpecificationsValidator<T>
{
    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(
        T entity, CancellationToken cancellationToken = default)
    {
        List<SpecificationError> errors = [];

        var unconditionalSpecifications =
            specifications.Where(spec => !spec.EvaluateOnlyIfNoPreviousErrors);

        var conditionalSpecifications =
            specifications.Where(spec => spec.EvaluateOnlyIfNoPreviousErrors);

        foreach (var specification in unconditionalSpecifications)
        {
            errors.AddRange(await specification
                .ValidateAsync(entity, cancellationToken).ConfigureAwait(false));
        }

        if (errors.Count == 0)
        {
            foreach (var specification in conditionalSpecifications)
            {
                var specificationErrors = await specification
                    .ValidateAsync(entity, cancellationToken).ConfigureAwait(false);

                if (specificationErrors.Count != 0)
                {
                    errors.AddRange(specificationErrors);

                    break;
                }
            }
        }

        return new ValidationResult(errors);
    }
}
