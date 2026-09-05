namespace Persiltech.DomainValidation;

/// <summary>
/// Registro del validador de especificaciones en el contenedor de dependencias.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra <see cref="IDomainSpecificationsValidator{T}"/> como genérico abierto, con
    /// tiempo de vida <c>Scoped</c>.
    /// </summary>
    /// <remarks>
    /// El registro es idempotente: llamarlo dos veces no duplica el servicio, y si la
    /// aplicación ya tenía una implementación registrada, la conserva. Las especificaciones
    /// concretas las registra la aplicación consumidora, no este método.
    /// </remarks>
    /// <param name="services">Colección de servicios en la que se registra.</param>
    /// <returns>La misma colección, para poder encadenar el registro.</returns>
    public static IServiceCollection AddDomainSpecificationsValidator(
        this IServiceCollection services)
    {
        services.TryAddScoped(typeof(IDomainSpecificationsValidator<>),
            typeof(DomainSpecificationsValidator<>));

        return services;
    }
}
