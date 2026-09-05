namespace Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;

/// <summary>
/// Regla que valida cada elemento de una colección con el validador de su propio tipo.
/// </summary>
public static class SetValidatorExtension
{
    /// <summary>
    /// Aplica el validador del tipo del elemento a cada elemento de la colección.
    /// </summary>
    /// <remarks>
    /// Los errores llegan con la ruta del elemento incorporada al nombre de la propiedad
    /// (Ej. <c>OrderDetails[0].Quantity</c>), de modo que el consumidor sabe qué elemento
    /// falló. Una colección <see langword="null"/> no produce errores: si además es
    /// obligatoria, encadena <c>NotEmpty</c>.
    /// </remarks>
    /// <typeparam name="T">Tipo de la entidad que contiene la colección.</typeparam>
    /// <typeparam name="TElement">Tipo de los elementos de la colección.</typeparam>
    /// <param name="tree">Árbol de reglas de la propiedad.</param>
    /// <param name="validator">Validador que evalúa cada elemento.</param>
    /// <returns>El mismo árbol, para encadenar más reglas.</returns>
    public static PropertySpecificationsTree<T, IEnumerable<TElement>>
        SetValidator<T, TElement>(
        this PropertySpecificationsTree<T, IEnumerable<TElement>> tree,
        IDomainSpecificationsValidator<TElement> validator) =>
        tree.AddAsyncRule(async (errors, value, cancellationToken) =>
        {
            if (value is null)
                return;

            int index = 0;

            foreach (var item in value)
            {
                var result = await validator
                    .ValidateAsync(item, cancellationToken).ConfigureAwait(false);

                foreach (var error in result.Errors)
                {
                    errors.Add(new SpecificationError(
                        $"{tree.PropertyName}[{index}].{error.PropertyName}",
                        error.ErrorMessage));
                }

                index++;
            }
        });
}
