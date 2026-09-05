namespace Persiltech.DomainValidation.Guards;

/// <summary>
/// Guarda que corta el flujo del caso de uso cuando la entidad no cumple sus reglas.
/// </summary>
public static class DomainValidationGuard
{
    /// <summary>
    /// Valida la entidad y lanza si no cumple.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad que se valida.</typeparam>
    /// <param name="validator">Validador que evalúa las especificaciones de la entidad.</param>
    /// <param name="entity">Entidad que se valida.</param>
    /// <param name="message">Mensaje de la excepción, opcional.</param>
    /// <param name="cancellationToken">Token que cancela la validación.</param>
    /// <exception cref="DomainValidationException">
    /// Si la entidad no cumple sus reglas. Los errores viajan en
    /// <see cref="DomainValidationException.Errors"/>.
    /// </exception>
    public static async Task AgainstInvalidSpecification<T>(
        IDomainSpecificationsValidator<T> validator, T entity,
        string? message = null, CancellationToken cancellationToken = default)
    {
        var validationResult = await validator
            .ValidateAsync(entity, cancellationToken).ConfigureAwait(false);

        if (!validationResult.IsValid)
            throw new DomainValidationException(
                validationResult.Errors, message);
    }
}
