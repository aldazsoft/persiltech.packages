namespace Persiltech.Membership.Sample.Configurations;

internal static class OpenApiConfiguration
{
    /// <summary>
    /// El paquete anota sus endpoints; renderizarlos es del consumidor. El transformador
    /// añade el esquema bearer, que OpenAPI no deduce del esquema de autenticación
    /// registrado.
    /// </summary>
    internal static WebApplicationBuilder AddCustomOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return builder;
    }

    /// <summary>
    /// Monta el documento y la interfaz de documentación.
    /// </summary>
    internal static WebApplication UseCustomOpenApi(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}
