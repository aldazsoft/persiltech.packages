namespace Persiltech.Membership.Sample.Configurations;

internal static class CorsConfiguration
{
    internal const string FrontendPolicy = "frontend";

    /// <summary>
    /// Abre la API al frontend de ejemplo, que vive en otro origen.
    /// </summary>
    /// <remarks>
    /// El paquete no configura CORS: es una decisión de despliegue del consumidor, que es
    /// quien sabe qué orígenes tienen derecho a llamarle. Los orígenes salen de la
    /// configuración, y no se usa <c>AllowAnyOrigin</c> porque con credenciales el navegador
    /// lo rechaza y porque una API de autenticación no debería aceptar a cualquiera.
    /// </remarks>
    /// <param name="builder">Constructor de la aplicación.</param>
    /// <returns>El mismo constructor, para poder encadenar.</returns>
    internal static WebApplicationBuilder AddCustomCors(this WebApplicationBuilder builder)
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["https://localhost:7195", "http://localhost:5195"];

        builder.Services.AddCors(options => options.AddPolicy(
            FrontendPolicy,
            policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return builder;
    }

    /// <summary>
    /// Aplica la política. Va antes de la autenticación: si no, una petición rechazada por
    /// falta de token volvería sin las cabeceras de CORS y el navegador mostraría un error
    /// de origen en lugar del 401 real.
    /// </summary>
    /// <param name="app">Aplicación.</param>
    /// <returns>La misma aplicación, para poder encadenar.</returns>
    internal static WebApplication UseCustomCors(this WebApplication app)
    {
        app.UseCors(FrontendPolicy);

        return app;
    }
}
