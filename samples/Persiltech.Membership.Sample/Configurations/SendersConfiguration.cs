namespace Persiltech.Membership.Sample.Configurations;

internal static class SendersConfiguration
{
    /// <summary>
    /// Los puertos de salida son del consumidor: el paquete no registra ninguna
    /// implementación, para que un olvido de configuración no se convierta en avisos que
    /// nadie envía.
    /// </summary>
    internal static WebApplicationBuilder AddMessageSenders(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMembershipEmailSender, LoggingEmailSender>();
        builder.Services.AddScoped<IMembershipSmsSender, LoggingSmsSender>();

        return builder;
    }
}
