namespace Persiltech.DomainValidation.ValueObjects;

/// <summary>
/// Error producido por una regla que la entidad no satisfizo.
/// </summary>
/// <param name="propertyName">Propiedad que incumple la regla.</param>
/// <param name="errorMessage">Mensaje que explica el incumplimiento.</param>
public class SpecificationError(string propertyName, string errorMessage)
{
    /// <summary>
    /// Propiedad que incumple la regla. En los elementos de una colección validada con
    /// <c>SetValidator</c> incluye la posición y la propiedad del elemento
    /// (Ej. <c>OrderDetails[0].Quantity</c>).
    /// </summary>
    public string PropertyName => propertyName;

    /// <summary>
    /// Mensaje que explica el incumplimiento, ya sea el predeterminado de la regla o el que
    /// se le pasó al declararla.
    /// </summary>
    public string ErrorMessage => errorMessage;
}
