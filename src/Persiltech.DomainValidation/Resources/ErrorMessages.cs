namespace Persiltech.DomainValidation.Resources;

/// <summary>
/// Mensajes de error predeterminados de las reglas, resueltos en el idioma de la aplicación.
/// </summary>
/// <remarks>
/// Los que llevan un dato —longitud, valor de comparación, expresión regular— traen un
/// marcador que la regla compone con <see cref="string.Format(string, object?)"/>. Cada regla
/// acepta un mensaje propio que sustituye al predeterminado.
/// </remarks>
public sealed class ErrorMessages
{
    // No es una clase estática porque Persiltech.Localizer la usa como argumento de tipo
    // genérico para localizar el recurso, y un tipo estático no puede serlo. El constructor
    // privado deja el efecto práctico: nadie la instancia.
    ErrorMessages() { }

    /// <summary>Mensaje predeterminado de la regla <c>EmailAddress</c>.</summary>
    public static string EmailAddress =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(EmailAddress));

    /// <summary>Mensaje predeterminado de la regla <c>Equal</c>.</summary>
    public static string Equal =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(Equal));

    /// <summary>Mensaje predeterminado de la regla <c>GreaterThan</c>, con el valor de comparación.</summary>
    public static string GreaterThan =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(GreaterThan));

    /// <summary>Mensaje predeterminado de la regla <c>GreaterThanOrEqualTo</c>, con el valor de comparación.</summary>
    public static string GreaterThanOrEqualTo =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(GreaterThanOrEqualTo));

    /// <summary>Mensaje predeterminado de la regla <c>HasFixedLength</c>, con la longitud exigida.</summary>
    public static string HasFixedLength =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(HasFixedLength));

    /// <summary>Mensaje predeterminado de la regla <c>HasMaxLength</c>, con la longitud máxima.</summary>
    public static string HasMaxLength =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(HasMaxLength));

    /// <summary>Mensaje predeterminado de la regla <c>HasMinLength</c>, con la longitud mínima.</summary>
    public static string HasMinLength =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(HasMinLength));

    /// <summary>Mensaje predeterminado de la regla <c>IsRequired</c>.</summary>
    public static string IsRequired =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(IsRequired));

    /// <summary>Mensaje predeterminado de la regla <c>Matches</c>, con la expresión regular.</summary>
    public static string Matches =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(Matches));

    /// <summary>Mensaje predeterminado de la regla <c>NotEmpty</c>.</summary>
    public static string NotEmpty =>
        LocalizationUtils<ErrorMessages>.GetValue(nameof(NotEmpty));
}
