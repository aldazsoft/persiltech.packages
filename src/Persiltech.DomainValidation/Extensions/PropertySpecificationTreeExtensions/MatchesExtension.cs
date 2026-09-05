namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que exige que una cadena case con una expresión regular.
/// </summary>
public static class MatchesExtension
{
    static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Exige que la cadena case con la expresión regular indicada. Un valor
    /// <see langword="null"/> se rechaza.
    /// </summary>
    /// <remarks>
    /// La expresión se analiza al declarar la regla, no en cada validación, y cada evaluación
    /// tiene un límite de un segundo para que un patrón de retroceso catastrófico no cuelgue
    /// el hilo.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="regularExpression">Expresión regular que la cadena debe satisfacer.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    /// <exception cref="ArgumentException">Si la expresión regular no es válida.</exception>
    /// <exception cref="RegexMatchTimeoutException">
    /// Si una evaluación excede el tiempo límite.
    /// </exception>
    public static PropertySpecificationsTree<T, string> Matches<T>(
        this PropertySpecificationsTree<T, string> tree,
        string regularExpression, string? errorMessage = null)
    {
        var regex = new Regex(regularExpression, RegexOptions.None, MatchTimeout);

        return tree.AddRule((errors, value) =>
        {
            if (value == null || !regex.IsMatch(value))
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ??
                    string.Format(ErrorMessages.Matches, regularExpression)));
            }
        });
    }
}
