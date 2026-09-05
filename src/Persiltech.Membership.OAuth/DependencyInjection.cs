namespace Persiltech.Membership.OAuth;

/// <summary>
/// Registro en el contenedor del servidor de autorización.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el contexto de datos de OpenIddict y el servidor de autorización.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación consumidora.</param>
    /// <param name="configureDbContext">
    /// Elige el proveedor de Entity Framework Core del
    /// <see cref="MembershipOAuthDbContext"/>.
    /// </param>
    /// <param name="configureOptions">Rellena las <see cref="MembershipOAuthOptions"/>.</param>
    /// <param name="configureServer">
    /// Punto de extensión sobre el constructor de OpenIddict, para lo que el paquete no
    /// decide: certificados de firma y cifrado propios, flujos adicionales, o cualquier
    /// ajuste del servidor. Se aplica <em>después</em> de la configuración del paquete, de
    /// modo que puede sobrescribirla.
    /// </param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/>, <paramref name="configureDbContext"/> o
    /// <paramref name="configureOptions"/> es <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// No llama a <c>AddAuthentication</c> ni monta ningún esquema interactivo: la sesión
    /// de navegador y la pantalla de inicio de sesión son del consumidor, igual que en el
    /// paquete base la validación de los tokens.
    /// </remarks>
    public static IServiceCollection AddMembershipOAuthServer(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<MembershipOAuthOptions> configureOptions,
        Action<OpenIddictServerBuilder>? configureServer = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<MembershipOAuthOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = new MembershipOAuthOptions();
        configureOptions(options);

        services.AddDbContext<MembershipOAuthDbContext>(configureDbContext);

        services.AddOpenIddict()
            .AddCore(core => core
                .UseEntityFrameworkCore()
                .UseDbContext<MembershipOAuthDbContext>())
            .AddServer(server =>
            {
                server.SetAuthorizationEndpointUris(options.AuthorizationEndpointPath)
                    .SetTokenEndpointUris(options.TokenEndpointPath)
                    .SetUserInfoEndpointUris(options.UserInfoEndpointPath)
                    .SetEndSessionEndpointUris(options.EndSessionEndpointPath)
                    .SetRevocationEndpointUris(options.RevocationEndpointPath);

                server.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                server.RegisterScopes([Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, .. options.Scopes]);

                server.SetAccessTokenLifetime(TimeSpan.FromMinutes(options.AccessTokenLifetimeInMinutes))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(options.RefreshTokenLifetimeInDays));

                // Sin cifrar, el token de acceso es un JWT que cualquier middleware estándar
                // valida. OpenIddict lo cifra por defecto, y eso obligaría a todo servidor
                // de recursos a usar su validación propia; el paquete base ya emite JWT
                // legibles y esta decisión los mantiene intercambiables.
                server.DisableAccessTokenEncryption();

                if (options.UseDevelopmentCertificates)
                {
                    server.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

                // La revocación no lleva passthrough: OpenIddict la resuelve por completo
                // contra su propio almacén, y un manejador nuestro solo podría estorbar.
                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();

                configureServer?.Invoke(server);
            });

        return services;
    }
}
