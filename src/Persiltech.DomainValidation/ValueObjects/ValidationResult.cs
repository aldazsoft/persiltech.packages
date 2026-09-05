namespace Persiltech.DomainValidation.ValueObjects;

/// <summary>
/// Resultado de validar una entidad contra sus especificaciones.
/// </summary>
/// <param name="errors">Errores reunidos durante la validación.</param>
public class ValidationResult(IEnumerable<SpecificationError> errors)
{
    /// <summary>
    /// Errores reunidos durante la validación, de todas las especificaciones que la entidad
    /// no satisfizo.
    /// </summary>
    /// <remarks>
    /// Se materializan al construir el resultado: quien lo recibe consulta
    /// <see cref="IsValid"/> y esta propiedad varias veces, y una secuencia perezosa se
    /// recorrería entera en cada consulta.
    /// </remarks>
    public IReadOnlyList<SpecificationError> Errors { get; } = [.. errors ?? []];

    /// <summary>
    /// Indica si la entidad cumple todas sus reglas, es decir, si no se produjo ningún error.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
