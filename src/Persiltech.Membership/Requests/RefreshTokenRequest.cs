namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de las peticiones de renovación y de cierre de sesión.
/// </summary>
/// <remarks>
/// La propiedad es anulable y no lleva <c>required</c>: así un cuerpo al que le falte el
/// campo llega con <see langword="null"/> y el error sale como
/// <c>ValidationProblemDetails</c>, en lugar de fallar antes en la deserialización con una
/// forma distinta de la acordada.
/// </remarks>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Testigo de renovación que presenta el cliente. Obligatorio.
    /// </summary>
    [Required]
    public string? RefreshToken { get; init; }
}
