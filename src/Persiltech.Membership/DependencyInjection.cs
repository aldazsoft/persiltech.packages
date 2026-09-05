namespace Persiltech.Membership;

/// <summary>
/// Registro en el contenedor de los servicios del paquete.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el contexto de datos, ASP.NET Core Identity y la emisión de tokens de acceso.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación consumidora.</param>
    /// <param name="configureJwtOptions">
    /// Rellena las <see cref="JwtOptions"/> con las que se firma el token.
    /// </param>
    /// <param name="configureDbContext">
    /// Elige el proveedor de Entity Framework Core del <see cref="MembershipDbContext"/>.
    /// </param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// Alguno de los tres argumentos es <see langword="null"/>: sin ellos no hay ni proveedor
    /// de datos ni clave de firma, y es preferible fallar aquí que en la primera petición.
    /// </exception>
    /// <remarks>
    /// No llama a <c>AddAuthentication</c> ni a <c>AddJwtBearer</c>: el paquete emite el
    /// token, pero validarlo es del consumidor, que es quien sabe qué otros esquemas tiene.
    /// Usa <c>AddIdentityCore</c> y no <c>AddIdentity</c> por la misma razón, para no fijar
    /// el esquema de autenticación por defecto. <c>AddRoles</c> es lo que aporta
    /// <see cref="RoleManager{TRole}"/>, que <c>AddIdentityCore</c> por sí solo no registra,
    /// y va antes de <c>AddEntityFrameworkStores</c> para que este registre también el
    /// almacén de roles. <c>AddDefaultTokenProviders</c> es lo que puebla el
    /// <c>ProviderMap</c> de Identity: sin él, todo lo que genere un testigo —confirmar el
    /// correo, reiniciar la contraseña, el doble factor— lanza en tiempo de ejecución,
    /// porque <c>AddIdentityCore</c> no registra ningún proveedor por su cuenta.
    /// </remarks>
    public static IServiceCollection AddMembershipServices(
        this IServiceCollection services,
        Action<JwtOptions> configureJwtOptions,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureJwtOptions);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<MembershipDbContext>(configureDbContext);

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MembershipDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Configure(configureJwtOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();

        return services;
    }
}
