namespace Persiltech.Membership.Email;

/// <summary>
/// Registro en el contenedor de los servicios del paquete.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el adaptador de correo de Persiltech.Membership y su compositor de plantillas.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación consumidora.</param>
    /// <param name="configureOptions">
    /// Rellena las <see cref="MembershipEmailOptions"/> con la marca y las rutas de la
    /// aplicación cliente.
    /// </param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// Falta alguno de los dos argumentos: sin las opciones no hay ni marca ni rutas de
    /// vuelta, y es preferible fallar aquí que en el primer aviso.
    /// </exception>
    /// <remarks>
    /// No registra el transporte: <c>IEmailSender</c> lo aporta el consumidor —normalmente
    /// con <c>AddSmtpEmailSender</c> de <c>Persiltech.Email</c>—, porque elegir servidor y
    /// credenciales es suyo. El orden entre ambas llamadas da igual; lo que no puede es
    /// faltar una.
    /// <para>
    /// Las opciones se validan al arrancar, con un <see cref="IValidateOptions{TOptions}"/>
    /// propio y <c>ValidateOnStart()</c>.
    /// </para>
    /// <para>
    /// Tanto el compositor como el validador se registran con <c>TryAddSingleton</c>, de modo
    /// que una implementación propia registrada antes gana.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMembershipEmail(
        this IServiceCollection services,
        Action<MembershipEmailOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.TryAddSingleton<IValidateOptions<MembershipEmailOptions>, MembershipEmailOptionsValidator>();

        services.AddOptions<MembershipEmailOptions>()
            .Configure(configureOptions)
            .ValidateOnStart();

        services.TryAddSingleton<IEmailTemplateRenderer, EmbeddedTemplateRenderer>();
        services.AddScoped<IMembershipEmailSender, TemplatedMembershipEmailSender>();

        return services;
    }
}
