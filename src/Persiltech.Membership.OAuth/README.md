# Persiltech.Membership.OAuth

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Membership.OAuth.svg)](https://www.nuget.org/packages/Persiltech.Membership.OAuth/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Servidor de autorización OAuth 2.0 y OpenID Connect sobre [OpenIddict](https://documentation.openiddict.com/),
construido encima de `Persiltech.Membership`: emite testigos para las mismas cuentas de
ASP.NET Core Identity que administra el paquete base.

Admite el flujo **Authorization Code con PKCE**, **credenciales de cliente** y la
renovación por **refresh token**.

## Instalación

    dotnet add package Persiltech.Membership.OAuth

## El contrato

```csharp
public sealed class MembershipOAuthDbContext(DbContextOptions<MembershipOAuthDbContext> options)
    : DbContext(options);

public sealed class MembershipOAuthOptions
{
    public string AuthorizationEndpointPath { get; set; }        // "/connect/authorize"
    public string TokenEndpointPath { get; set; }                // "/connect/token"
    public string UserInfoEndpointPath { get; set; }             // "/connect/userinfo"
    public string EndSessionEndpointPath { get; set; }           // "/connect/logout"
    public string RevocationEndpointPath { get; set; }           // "/connect/revoke"
    public string LoginPath { get; set; }                        // "/account/login"
    public string InteractiveAuthenticationScheme { get; set; }  // "Cookies"
    public int AccessTokenLifetimeInMinutes { get; set; }        // 30
    public int RefreshTokenLifetimeInDays { get; set; }          // 14
    public string[] Scopes { get; set; }
    public bool UseDevelopmentCertificates { get; set; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddMembershipOAuthServer(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<MembershipOAuthOptions> configureOptions,
        Action<OpenIddictServerBuilder>? configureServer = null);
}

public static class OAuthEndpoints
{
    public static IEndpointRouteBuilder MapMembershipOAuthEndpoints(this IEndpointRouteBuilder endpoints);
}

public sealed record MembershipOAuthClient(
    string ClientId,
    string DisplayName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> Scopes,
    string? ClientSecret = null);

public static class MembershipOAuthClientRegistrar
{
    public static Task RegisterMembershipOAuthClientsAsync(
        this IServiceProvider provider,
        IReadOnlyList<MembershipOAuthClient> clients,
        CancellationToken cancellationToken = default);
}
```

Las rutas de los endpoints salen de las opciones y no de parámetros, a diferencia del
paquete base: tienen que coincidir con las que se declararon en OpenIddict, y admitir dos
fuentes para el mismo dato sería una forma de que dejaran de coincidir.

`MembershipOAuthDbContext` es un contexto **aparte** del `MembershipDbContext` del paquete
base. Aquel es `sealed` y no declara las entidades de OpenIddict, y ampliarlo obligaría al
paquete base a depender de OpenIddict aunque no se use. Los dos pueden apuntar a la misma
base de datos; cada uno lleva sus propias migraciones.

## Uso

```csharp
builder.Services.AddMembershipOAuthServer(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("Membership"),
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)),
    oauth =>
    {
        oauth.LoginPath = "/account/login";
        oauth.UseDevelopmentCertificates = true;   // solo en desarrollo
    });

// El esquema interactivo es tuyo: el flujo Authorization Code exige una sesión de
// navegador, y el paquete no monta ninguna ni trae pantalla de inicio de sesión.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.LoginPath = "/account/login");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMembershipOAuthEndpoints();

// Idempotente: vuelve a describir el cliente si ya estaba.
await app.Services.RegisterMembershipOAuthClientsAsync(
[
    new MembershipOAuthClient(
        ClientId: "mi-spa",
        DisplayName: "Mi aplicación",
        RedirectUris: ["https://localhost:7082/callback"],
        Scopes: ["openid", "email", "profile", "roles"])
]);
```

Un cliente **sin secreto** es público y solo puede usar Authorization Code con PKCE — es el
caso de una aplicación de navegador o móvil, que no puede guardar un secreto. Uno **con
secreto** es confidencial y puede usar además credenciales de cliente.

## Migraciones

El contexto es tuyo, igual que en el paquete base: eliges el proveedor y generas la
migración contra tu propio ensamblado.

    dotnet dotnet-ef migrations add InitialOAuth --context MembershipOAuthDbContext
    dotnet dotnet-ef database update --context MembershipOAuthDbContext

Con dos contextos, `dotnet ef` anida las migraciones de cada uno en su propio
subdirectorio. Si tu `.editorconfig` exime a `Migrations/` de las reglas de estilo,
comprueba que el patrón cubra también los subdirectorios (`**/Migrations/**.cs`).

## Los endpoints que monta

| Ruta                  | Método     | Anónimo | Quién lo atiende                               |
| --------------------- | ---------- | ------- | ---------------------------------------------- |
| `/connect/authorize`  | GET, POST  | Sí      | El paquete, tras comprobar la sesión de cookie |
| `/connect/token`      | POST       | Sí      | El paquete                                     |
| `/connect/userinfo`   | GET, POST  | **No**  | El paquete, con el token de acceso             |
| `/connect/logout`     | GET, POST  | Sí      | El paquete, cerrando la sesión interactiva     |
| `/connect/revoke`     | POST       | Sí      | **OpenIddict, por completo**                   |

La revocación no aparece en `MapMembershipOAuthEndpoints` a propósito: OpenIddict la
resuelve entera contra su propio almacén, y un manejador nuestro solo podría estorbar.
Basta con que la ruta esté declarada en las opciones.

`/connect/logout` es anónimo porque se invoca justo cuando la sesión puede haber caducado
ya; `/connect/userinfo` no lo es, porque su razón de ser es leer el token de acceso.

## Decisiones de diseño

- **El token de acceso no se cifra.** OpenIddict lo cifra por defecto, lo que obligaría a
  todo servidor de recursos a usar su validación propia. Sin cifrar es un JWT que cualquier
  middleware estándar valida, y se mantiene intercambiable con el que emite el paquete base.
  A cambio, su contenido es legible por quien lo tenga: no metas en él nada que no puedas
  enseñar.
- **PKCE es obligatorio** en el flujo Authorization Code, no opcional.
- **La pantalla de inicio de sesión y el esquema interactivo son tuyos.** El paquete no
  trae interfaz ni impone maquetación; solo redirige a `LoginPath` cuando no hay sesión.
- **El consentimiento es implícito.** Los clientes se registran como de confianza, que es
  lo razonable en aplicaciones propias. Una pantalla de consentimiento para clientes de
  terceros queda fuera de esta versión.
- **`configureServer` se aplica al final**, de modo que puedes sobrescribir cualquier
  decisión del paquete: certificados propios, flujos adicionales o ajustes del servidor.
- **Las cuentas bloqueadas no obtienen testigos**, ni al autorizar ni al renovar. Se
  comprueba en cada canje, no solo al emitir el código: una cuenta desactivada después de
  autorizarse deja de renovar.

## Compatibilidad

`net10.0`.

## Estado

Versión `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

El código fuente no es público, así que esta tabla es el registro de cambios del paquete.

| Versión | Cambios                                                                                     |
| ------- | ------------------------------------------------------------------------------------------- |
| 0.2.0   | Primera versión en nuget.org: servidor de autorización sobre OpenIddict con Authorization Code + PKCE, credenciales de cliente y refresh token, sobre las cuentas de `Persiltech.Membership`. |

La versión `0.1.0` fue interna y nunca llegó a nuget.org.

## Soporte

El código fuente de este paquete no es público. Para dudas, fallos o peticiones, usa la
[página del paquete](https://aldazsoft.github.io/Membership.OAuth/).

## Apoyar el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
