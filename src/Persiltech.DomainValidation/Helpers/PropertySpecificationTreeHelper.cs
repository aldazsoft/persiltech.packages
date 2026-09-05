namespace Persiltech.DomainValidation.Helpers;

internal static class PropertySpecificationTreeHelper
{
    // La propiedad puede venir sin valor: es justo lo que comprueban IsRequired y NotEmpty,
    // así que el nulo llega hasta la regla en lugar de cortarse aquí.
    public static PropertySpecificationsTree<T, TProperty> AddRule<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Action<List<SpecificationError>, TProperty> rule) =>
        tree.AddRule((errors, value, entity) => rule(errors, value));

    // Para las reglas que necesitan la entidad completa, como comparar contra otra propiedad.
    public static PropertySpecificationsTree<T, TProperty> AddRule<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Action<List<SpecificationError>, TProperty, T> rule) =>
        tree.Add(new Specification<T>(entity =>
        {
            List<SpecificationError> errors = [];

            rule(errors, tree.GetPropertyValue(entity), entity);

            return errors;
        }));

    public static PropertySpecificationsTree<T, TProperty> AddAsyncRule<T, TProperty>(
        this PropertySpecificationsTree<T, TProperty> tree,
        Func<List<SpecificationError>, TProperty, CancellationToken, ValueTask> rule) =>
        tree.Add(new AsyncSpecification<T>(async (entity, cancellationToken) =>
        {
            List<SpecificationError> errors = [];

            await rule(errors, tree.GetPropertyValue(entity), cancellationToken)
                .ConfigureAwait(false);

            return errors;
        }));
}
