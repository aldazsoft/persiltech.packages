namespace Persiltech.Results.Resources;

/// <summary>
/// Mensajes de las excepciones que lanza <see cref="Result{TSuccess, TError}"/> al leer una
/// propiedad que no corresponde a su estado, resueltos en el idioma de la aplicación.
/// </summary>
public class ResultMessages
{
    /// <summary>
    /// Mensaje al leer <c>Error</c> en un resultado correcto.
    /// </summary>
    public static string CannotAccessErrorWhenResultIsSuccess =>
        LocalizationUtils<ResultMessages>.GetValue(nameof(CannotAccessErrorWhenResultIsSuccess));

    /// <summary>
    /// Mensaje al leer <c>Value</c> en un resultado fallido.
    /// </summary>
    public static string CannotAccessValueWhenResultIsFailureMessage =>
        LocalizationUtils<ResultMessages>.GetValue(nameof(CannotAccessValueWhenResultIsFailureMessage));
}
