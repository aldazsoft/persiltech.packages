namespace Persiltech.UserServices;

/// <summary>
/// Métodos de extensión de <see cref="IServiceCollection"/> que registran el adaptador de
/// identidad basado en <c>HttpContext</c>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra <see cref="IHttpContextAccessor"/> y la implementación
    /// <see cref="HttpContextUserService"/> de <see cref="IUserService"/>.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <returns>La misma colección, para encadenar registros.</returns>
    /// <remarks>
    /// Ambos servicios se registran como <see cref="ServiceLifetime.Singleton"/>: el adaptador
    /// no tiene estado y es el accesor quien resuelve la petición en curso. El registro es
    /// idempotente y no sustituye una implementación de <see cref="IUserService"/> ya presente.
    /// </remarks>
    public static IServiceCollection AddHttpContextUserService(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IUserService, HttpContextUserService>();

        return services;
    }
}
