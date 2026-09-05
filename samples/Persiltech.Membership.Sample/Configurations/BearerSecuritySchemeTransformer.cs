namespace Persiltech.Membership.Sample.Configurations;

/// <summary>
/// Declara el esquema de seguridad <em>bearer</em> en el documento OpenAPI.
/// </summary>
/// <remarks>
/// <c>Microsoft.AspNetCore.OpenApi</c> no lo añade por su cuenta a partir del esquema de
/// autenticación registrado, así que sin este transformador la interfaz de documentación
/// no ofrece dónde pegar el token y hay que ponerlo a mano en cada petición.
/// <para>
/// Vive en el sample y no en el paquete a propósito: el paquete anota sus endpoints con
/// metadatos del framework, pero no decide con qué se renderizan ni qué esquemas de
/// autenticación tiene la aplicación que los monta.
/// </para>
/// </remarks>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Token de acceso emitido por el endpoint de autenticación."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = scheme;

        return Task.CompletedTask;
    }
}
