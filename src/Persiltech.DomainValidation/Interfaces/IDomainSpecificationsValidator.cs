namespace Persiltech.DomainValidation.Interfaces;

/// <summary>
/// Evalúa todas las especificaciones registradas para un tipo y reúne sus errores.
/// </summary>
/// <typeparam name="T">Tipo de la entidad que se valida.</typeparam>
public interface IDomainSpecificationsValidator<T>
{
    /// <summary>
    /// Valida la entidad contra todas sus especificaciones.
    /// </summary>
    /// <param name="entity">Entidad que se valida.</param>
    /// <param name="cancellationToken">Token que cancela la validación.</param>
    /// <returns>
    /// El resultado de la validación, con los errores reunidos de todas las especificaciones
    /// que la entidad no satisfizo.
    /// </returns>
    Task<ValidationResult> ValidateAsync(
        T entity, CancellationToken cancellationToken = default);
}
