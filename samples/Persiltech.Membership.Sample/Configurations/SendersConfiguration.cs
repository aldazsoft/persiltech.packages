namespace Persiltech.Membership.Sample.Configurations;

internal static class SendersConfiguration
{
    /// <summary>
    /// Cierra los puertos de salida del paquete base: el correo con SMTP real y el SMS con
    /// el log.
    /// </summary>
    /// <remarks>
    /// Los puertos son del consumidor: el paquete no registra ninguna implementación, para
    /// que un olvido de configuración no se convierta en avisos que nadie envía.
    /// <para>
    /// El correo se compone en tres capas, y cada una es un paquete distinto:
    /// <c>Persiltech.Membership</c> entrega los datos y el testigo por
    /// <c>IMembershipEmailSender</c>; <c>Persiltech.Membership.Email</c> los convierte en un
    /// mensaje con sus plantillas; y <c>Persiltech.Email</c> lo entrega por SMTP. Aquí solo
    /// se enchufan.
    /// </para>
    /// <para>
    /// El SMS se queda en el log: no hay un MailHog equivalente para SMS, y montar una
    /// pasarela de verdad en un ejemplo pediría credenciales y coste por mensaje.
    /// </para>
    /// </remarks>
    /// <param name="builder">Constructor de la aplicación.</param>
    /// <returns>El mismo constructor, para poder encadenar.</returns>
    internal static WebApplicationBuilder AddMessageSenders(this WebApplicationBuilder builder)
    {
        builder.Services.AddSmtpEmailSender(
            builder.Configuration.GetSection("Smtp").Bind);

        builder.Services.AddMembershipEmail(
            builder.Configuration.GetSection("MembershipEmail").Bind);

        builder.Services.AddScoped<IMembershipSmsSender, LoggingSmsSender>();

        return builder;
    }
}
