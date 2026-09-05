namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que comprueba que una cadena tenga forma de dirección de correo.
/// </summary>
public static partial class EmailAddressExtension
{
    /// <summary>
    /// Exige que la cadena tenga forma de dirección de correo. Un valor
    /// <see langword="null"/> o en blanco se rechaza.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="errorMessage">Mensaje propio que sustituye al predeterminado.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, string> EmailAddress<T>(
        this PropertySpecificationsTree<T, string> tree,
        string? errorMessage = null) =>
        tree.AddRule((errors, value) =>
        {
            if (string.IsNullOrWhiteSpace(value) || !EmailRegex().IsMatch(value))
            {
                errors.Add(new SpecificationError(tree.PropertyName,
                    errorMessage ?? ErrorMessages.EmailAddress));
            }
        });

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
