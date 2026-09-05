namespace Persiltech.Email;

/// <summary>
/// Registro en el contenedor de los servicios del paquete.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra <see cref="IEmailSender"/> con la implementación SMTP.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación consumidora.</param>
    /// <param name="configureOptions">
    /// Rellena las <see cref="SmtpOptions"/> con las que se conecta al servidor.
    /// </param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// Falta alguno de los dos argumentos: sin las opciones no hay servidor al que conectar,
    /// y es preferible fallar aquí que en el primer envío.
    /// </exception>
    /// <remarks>
    /// Las opciones se validan al arrancar, con un <see cref="IValidateOptions{TOptions}"/>
    /// propio y <c>ValidateOnStart()</c>. De dónde salgan sus valores es del consumidor: el
    /// delegado admite tanto literales como el enlace de su configuración.
    /// <para>
    /// El validador se registra con <c>TryAddSingleton</c>, de modo que uno propio
    /// registrado antes gana.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSmtpEmailSender(
        this IServiceCollection services,
        Action<SmtpOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.TryAddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();

        services.AddOptions<SmtpOptions>()
            .Configure(configureOptions)
            .ValidateOnStart();

        services.AddSingleton<ISmtpClientFactory, SmtpClientFactory>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
