# Superficie pública

Seis tipos, todos en `Persiltech.Membership.OAuth`.

## Registro

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddMembershipOAuthServer(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<MembershipOAuthOptions> configureOptions,
        Action<OpenIddictServerBuilder>? configureServer = null);
}
```

`configureServer` se aplica **después** de la configuración del paquete, así que puede
sobrescribirla: certificados propios, flujos adicionales o cualquier ajuste del servidor.
Es el punto por el que un consumidor añade lo que el paquete no decide.

Los tres primeros parámetros son obligatorios y se comprueban con
`ArgumentNullException.ThrowIfNull`.

## Endpoints

```csharp
public static class OAuthEndpoints
{
    public static IEndpointRouteBuilder MapMembershipOAuthEndpoints(this IEndpointRouteBuilder endpoints);
}
```

Monta cuatro rutas, tomadas de `MembershipOAuthOptions` —no de parámetros, porque tienen que
coincidir con las que se declararon en OpenIddict—:

| Ruta | Verbos | Autorización |
| --- | --- | --- |
| `AuthorizationEndpointPath` | GET, POST | Anónima |
| `TokenEndpointPath` | POST | Anónima |
| `UserInfoEndpointPath` | GET, POST | Requiere token |
| `EndSessionEndpointPath` | GET, POST | Anónima |

La revocación se declara en OpenIddict pero no se monta aquí: la resuelve él por completo.

## Opciones

```csharp
public sealed class MembershipOAuthOptions
{
    public string AuthorizationEndpointPath { get; set; } = "/connect/authorize";
    public string TokenEndpointPath { get; set; } = "/connect/token";
    public string UserInfoEndpointPath { get; set; } = "/connect/userinfo";
    public string EndSessionEndpointPath { get; set; } = "/connect/logout";
    public string RevocationEndpointPath { get; set; } = "/connect/revoke";
    public string LoginPath { get; set; } = "/account/login";
    public string InteractiveAuthenticationScheme { get; set; }
    public int AccessTokenLifetimeInMinutes { get; set; } = 30;
    public int RefreshTokenLifetimeInDays { get; set; } = 14;
    public string[] Scopes { get; set; } = [];
    public bool UseDevelopmentCertificates { get; set; }
}
```

Se validan con `ValidateDataAnnotations().ValidateOnStart()`: una ruta mal escrita falla el
arranque, no la primera petición.

`Scopes` añade ámbitos propios a los cuatro que el paquete registra siempre: `openid`,
`email`, `profile` y `roles`.

## Clientes

```csharp
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

Se invoca al arrancar, después de aplicar las migraciones. Es idempotente: vuelve a describir
el cliente si ya estaba, así que ejecutarlo en cada arranque deja el registro al día.

`RedirectUris` se comparan de forma **exacta**. Es lo que impide que un tercero que conozca el
identificador del cliente se lleve el código de autorización a su propio dominio.

`ClientSecret` decide el tipo de cliente y, con él, los flujos permitidos:

| | Sin secreto (público) | Con secreto (confidencial) |
| --- | --- | --- |
| Authorization Code + PKCE | sí | sí |
| Refresh token | sí | sí |
| Credenciales de cliente | **no** | sí |

## Contexto de datos

```csharp
public sealed class MembershipOAuthDbContext(DbContextOptions<MembershipOAuthDbContext> options);
```

Guarda las entidades de OpenIddict —aplicaciones, autorizaciones, ámbitos y testigos— y no
comparte ninguna tabla con `MembershipDbContext`. Sus migraciones se generan con `--context`
y en su propia carpeta; los comandos están en el `README.md` de la raíz del repositorio.

## Respuestas de error

Los rechazos siguen el RFC 6749, no una convención propia:

| Situación | Código | `error` |
| --- | --- | --- |
| Secreto o cliente inválido | **401** | `invalid_client` |
| Concesión no admitida | 400 | `unsupported_grant_type` |
| Código repetido, caducado o con verificador equivocado | 400 | `invalid_grant` |
| Token de acceso inválido en `userinfo` | 401 | `invalid_token` |
| Cuenta bloqueada o inexistente al autorizar | 403 | `access_denied` |

Repetir un código de autorización no solo devuelve `invalid_grant`: OpenIddict revoca **toda
la cadena de testigos** que salió de ese código, incluido el de renovación. Es lo correcto —si
alguien más tuvo el código, la sesión está comprometida— pero conviene saberlo al probar.
