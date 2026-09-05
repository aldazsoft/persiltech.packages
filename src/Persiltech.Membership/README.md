# Persiltech.Membership

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Membership.svg)](https://www.nuget.org/packages/Persiltech.Membership/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Sistema de membresía reutilizable para aplicaciones ASP.NET Core: registro y autenticación
de usuarios sobre ASP.NET Core Identity, con endpoints de Minimal API que se montan en la
ruta que elijas y emisión de un JSON Web Token firmado con HMAC-SHA256.

## Instalación

```
dotnet add package Persiltech.Membership
```

## El contrato

```csharp
public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}

public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);

public sealed class JwtOptions
{
    [Required]
    [MinLength(32)]
    public string SecurityKey { get; set; } = string.Empty;

    [Required]
    public string ValidIssuer { get; set; } = string.Empty;

    [Required]
    public string ValidAudience { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpireInMinutes { get; set; }
}

public sealed record RegisterUserRequest
{
    [Required][EmailAddress] public string? Email { get; init; }
    [Required] public string? Password { get; init; }
    [Required][MaxLength(100)] public string? FirstName { get; init; }
    [Required][MaxLength(100)] public string? LastName { get; init; }
}

public sealed record LoginUserRequest
{
    [Required][EmailAddress] public string? Email { get; init; }
    [Required] public string? Password { get; init; }
}

public sealed record LoginUserResponse(string AccessToken);

public static class DependencyInjection
{
    public static IServiceCollection AddMembershipServices(
        this IServiceCollection services,
        Action<JwtOptions> configureJwtOptions,
        Action<DbContextOptionsBuilder> configureDbContext);
}

public static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(
        this IEndpointRouteBuilder endpoints,
        string registrationPattern = "user/register",
        string loginPattern = "user/login");

    public static RouteHandlerBuilder MapUserRegistrationEndpoint(
        this IEndpointRouteBuilder endpoints, string pattern);

    public static RouteHandlerBuilder MapUserLoginEndpoint(
        this IEndpointRouteBuilder endpoints, string pattern);
}
```

Las propiedades de `RegisterUserRequest` y `LoginUserRequest` son **anulables y sin
`required`** a propósito. Con `required`, a un cuerpo JSON al que le falte un campo le
fallaría la deserialización antes de correr ninguna validación, y el cliente recibiría un
error con una forma distinta de la acordada. Siendo anulables, el campo ausente llega como
`null`, `[Required]` lo rechaza y el error sale como `ValidationProblemDetails`.

`ApplicationUser` y `MembershipDbContext` son públicos porque los necesitas para generar
las migraciones y para resolver `UserManager<ApplicationUser>` desde tu propio código. Sus
anotaciones no validan peticiones: describen la columna que genera Entity Framework Core
(`NOT NULL`, `nvarchar(100)`).

`JwtOptions` se valida **al arrancar la aplicación**, no en la primera petición:
`AddMembershipServices` encadena `ValidateDataAnnotations().ValidateOnStart()`. El mínimo
de 32 caracteres de `SecurityKey` no es arbitrario — HMAC-SHA256 exige una clave de al
menos 256 bits — y convierte en un fallo de arranque lo que si no sería una excepción al
emitir el primer token.

`MapMembershipEndpoints` no hace nada que no puedas hacer llamando a los otros dos métodos:
es el atajo para el caso corriente. Devuelve el `IEndpointRouteBuilder` porque monta dos
rutas y ninguna representaría a la otra; los métodos individuales sí devuelven el
`RouteHandlerBuilder`, para que decores cada endpoint por separado.

### Roles y usuarios

Desde la 0.2.0 el paquete administra también roles y usuarios, en dos grupos aparte:

```csharp
public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "roles");
    public static RouteHandlerBuilder MapCreateRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapUpdateRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapDeleteRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapGetRoleByIdEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapGetRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapGetPagedRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
}

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "users");
    public static RouteHandlerBuilder MapGetCurrentUserEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapGetUserByIdEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapGetPagedUsersEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapUpdateUserStatusEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
    public static RouteHandlerBuilder MapAssignUserRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern);
}
```

Con sus cuerpos y respuestas:

```csharp
public sealed record CreateRoleRequest { [Required][MaxLength(256)] public string? Name { get; init; } }
public sealed record UpdateRoleRequest { [Required][MaxLength(256)] public string? Name { get; init; } }
public sealed record RoleResponse(string Id, string Name);

public sealed record UpdateUserStatusRequest { [Required] public bool? IsActive { get; init; } }
public sealed record AssignRolesRequest { [Required] public string[]? Roles { get; init; } }

public sealed record UserResponse(
    string Id, string Email, string FirstName, string LastName,
    bool EmailConfirmed, bool IsActive, IReadOnlyList<string> Roles);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages { get; }
}
```

**Ninguno de estos endpoints es anónimo, y ninguno fija una política.** Son operaciones de
administración, pero el paquete no sabe qué políticas tienes; por eso cada método devuelve
su `RouteHandlerBuilder` y la política la encadenas tú. Para aplicarla a todo el grupo de
una vez, móntalos sobre un `MapGroup`:

```csharp
var administration = app.MapGroup(string.Empty).RequireAuthorization("Administrators");

administration.MapRoleEndpoints();
administration.MapUserEndpoints();
```

`AssignRolesRequest.Roles` **sustituye** la lista anterior: el usuario queda exactamente
con los roles indicados, y un arreglo vacío lo deja sin ninguno. Si alguno no existe, no se
aplica ninguno y la respuesta es `400`.

### Contraseñas, correo y teléfono

Desde la 0.3.0, en tres grupos más:

```csharp
public static class PasswordEndpoints
{
    public static IEndpointRouteBuilder MapPasswordEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "password");
    // POST password/change · password/forgot · password/reset
}

public static class EmailEndpoints
{
    public static IEndpointRouteBuilder MapEmailEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "email");
    // POST email/confirmation/send · email/confirmation · email/change · email/change/confirm
}

public static class PhoneNumberEndpoints
{
    public static IEndpointRouteBuilder MapPhoneNumberEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "phone");
    // POST phone/change · phone/change/confirm
}
```

Cada grupo trae también un método por endpoint, para montarlos sueltos.

### Los puertos de salida los implementas tú

El paquete **no envía correos ni SMS, y no redacta ningún mensaje**. Entrega los datos y el
testigo por dos puertos que implementas y registras en tu contenedor:

```csharp
public interface IMembershipEmailSender
{
    Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken);
    Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken);
}

public interface IMembershipSmsSender
{
    Task SendPhoneChangeAsync(PhoneChangeMessage message, CancellationToken cancellationToken);
}

public sealed record EmailConfirmationMessage(string UserId, string Email, string FirstName, string LastName, string Token);
public sealed record PasswordResetMessage(string UserId, string Email, string FirstName, string LastName, string Token);
public sealed record EmailChangeMessage(string UserId, string NewEmail, string FirstName, string LastName, string Token);
public sealed record PhoneChangeMessage(string UserId, string PhoneNumber, string FirstName, string LastName, string Token);
```

Redactar el mensaje aquí obligaría al paquete a elegir plantilla, formato e idioma, y a
inventarse el patrón de ruta de la pantalla que recibe el testigo, que es de tu aplicación.
`AddMembershipServices` **no registra implementación de reserva** a propósito: una que no
enviara nada convertiría un olvido de configuración en un fallo silencioso.

`IMembershipSmsSender` solo hace falta si montas los endpoints de teléfono.

Si no quieres redactar los correos a mano,
[Persiltech.Membership.Email](https://www.nuget.org/packages/Persiltech.Membership.Email/)
implementa `IMembershipEmailSender` con plantillas HTML —encabezado, cuerpo y pie— que
rebrandeas por configuración, y los entrega por
[Persiltech.Email](https://www.nuget.org/packages/Persiltech.Email/).

### Perfil y doble factor

Desde la 0.5.0, en dos grupos más. Los seis operan sobre la cuenta autenticada:

```csharp
public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "profile");
    // PUT profile · DELETE profile
}

public static class TwoFactorEndpoints
{
    public static IEndpointRouteBuilder MapTwoFactorEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "twofactor");
    // POST twofactor/setup · enable · disable · recovery-codes
}

public sealed record UpdateProfileRequest { public string? FirstName { get; init; } public string? LastName { get; init; } }
public sealed record EnableTwoFactorRequest { public string? Code { get; init; } }
public sealed record TwoFactorSetupResponse(string SharedKey, string Email);
public sealed record TwoFactorRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
```

**La baja borra la cuenta**, a diferencia de la operación de administración, que solo la
desactiva. Son dos cosas distintas a propósito: un administrador suspende a un tercero y
quiere poder revertirlo; el titular que se da de baja pide que sus datos dejen de estar.
Identity borra en cascada roles, reclamaciones e inicios de sesión externos; lo que el
paquete no puede tocar son las autorizaciones y testigos de un servidor OAuth, que viven en
otro contexto de datos.

**El segundo factor viaja en la propia petición de autenticación**, en
`LoginUserRequest.TwoFactorCode`, y no en una segunda llamada:

```json
{ "email": "…", "password": "…", "twoFactorCode": "123456" }
```

Así el contrato de la respuesta no cambia: quien ya consumía `user/login` no toca nada
mientras no active el doble factor. En una cuenta con doble factor, un código ausente o
inválido responde **400** con la clave `twoFactorCode` — aquí sí se señala el campo, porque
la contraseña ya se comprobó y decirlo no filtra nada. Vale tanto el código de la
aplicación de autenticación como uno de recuperación, que se consume al usarlo.

`TwoFactorSetupResponse` devuelve la clave compartida y el correo, **no la URI
`otpauth://` ni el QR**: esa URI lleva el nombre del emisor, que es tu marca, y componerla
aquí obligaría al paquete a decidirla. Es la misma frontera que con los mensajes de correo.

### El administrador inicial

Desde la 0.4.0. Sin esto una instalación nueva queda en punto muerto: los endpoints de
administración exigen la política que pongas tú, y el endpoint que crearía el primer rol de
administrador exigiría ya serlo.

```csharp
public sealed record MembershipAdministrator(
    string Email, string Password, string FirstName, string LastName,
    string RoleName = "Administrator");

public static class MembershipSeeder
{
    public static Task<bool> SeedMembershipAdministratorAsync(
        this IServiceProvider provider,
        MembershipAdministrator administrator,
        CancellationToken cancellationToken = default);
}
```

```csharp
await app.Services.SeedMembershipAdministratorAsync(
    new MembershipAdministrator(
        builder.Configuration["Administrator:Email"]!,
        builder.Configuration["Administrator:Password"]!,
        "Ada", "Lovelace"));
```

Devuelve `true` solo si esta llamada creó la cuenta. Es idempotente y **no toca la cuenta
si ya existe**: en particular, no reescribe su contraseña, de modo que dejar la llamada en
el arranque no revierte en cada despliegue la que el administrador haya cambiado. Si algo
falla lanza `InvalidOperationException` en lugar de devolver un resultado: ocurre en el
arranque, y una instalación sin administrador no debe quedarse en pie disimulándolo.

La contraseña es un secreto: sácala de tu configuración, no del código.

## Uso

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMembershipServices(
    jwt => builder.Configuration.GetSection("Jwt").Bind(jwt),
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("Membership"),
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)));

// El paquete emite el token; validarlo es tuyo.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
        ValidAudience = builder.Configuration["Jwt:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecurityKey"]!))
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMembershipEndpoints();

app.Run();
```

Con las rutas por defecto o con las tuyas, y decorando cada endpoint si hace falta:

```csharp
app.MapMembershipEndpoints();                             // user/register y user/login
app.MapMembershipEndpoints("auth/signup", "auth/signin"); // las tuyas

app.MapUserRegistrationEndpoint("user/register").WithTags("Cuentas");
app.MapUserLoginEndpoint("user/login").RequireRateLimiting("login");
```

La configuración correspondiente:

```json
{
  "ConnectionStrings": {
    "Membership": "Server=(localdb)\\MSSQLLocalDB;Database=Membership;Trusted_Connection=True"
  },
  "Jwt": {
    "SecurityKey": "clave-local-de-ejemplo-de-32-caracteres",
    "ValidIssuer": "https://localhost:7082",
    "ValidAudience": "mi-aplicacion",
    "ExpireInMinutes": 30
  }
}
```

## Contratos HTTP

Las dos respuestas de error son `ValidationProblemDetails` con **400 Bad Request**, con las
claves de `errors` en camelCase, igual que los campos del JSON al que se refieren. Los
errores que no corresponden a un campo concreto usan la clave vacía `""`.

### Registro

`POST user/register`

```json
{
  "email": "juan.perez@example.com",
  "password": "Passw0rd!",
  "firstName": "Juan",
  "lastName": "Pérez"
}
```

| Resultado | Respuesta                                             |
| --------- | ----------------------------------------------------- |
| Correcto  | **201 Created**, sin cuerpo y sin cabecera `Location` |
| Error     | **400 Bad Request** con `ValidationProblemDetails`    |

No hay `Location` porque el paquete no expone ningún endpoint que devuelva el usuario
creado: apuntar a una ruta inexistente sería peor que omitir la cabecera.

| Caso                                                  | Clave en `errors`                 |
| ----------------------------------------------------- | --------------------------------- |
| Campo ausente, vacío o con formato de correo inválido | el campo (`email`, `password`, …) |
| Contraseña que no cumple la política de Identity      | `password`                        |
| Correo ya registrado                                  | `email`                           |
| Cualquier otro error de Identity                      | `""`                              |

La cuenta se crea con `UserName` y `Email` iguales al correo recibido, y **sin confirmación
de correo**: queda utilizable de inmediato.

### Autenticación

`POST user/login`

```json
{ "email": "juan.perez@example.com", "password": "Passw0rd!" }
```

| Resultado | Respuesta                                                     |
| --------- | ------------------------------------------------------------- |
| Correcto  | **200 OK** con `{ "accessToken": "eyJhbGciOiJIUzI1NiIs..." }` |
| Error     | **400 Bad Request** con `ValidationProblemDetails`            |

El correo inexistente y la contraseña errónea comparten respuesta **exacta** —clave vacía y
mensaje `Credenciales inválidas.`— y es deliberado: distinguirlos convertiría el endpoint en
un verificador de qué correos están registrados. Una cuenta desactivada recibe esa misma
respuesta.

### Roles

| Operación               | Petición              | Correcto                                     | Errores                              |
| ----------------------- | --------------------- | -------------------------------------------- | ------------------------------------ |
| `POST {pattern}`        | `CreateRoleRequest`   | **201** con `RoleResponse` y `Location`      | **400**                              |
| `PUT {pattern}/{id}`    | `UpdateRoleRequest`   | **200** con `RoleResponse`                   | **400**, **404**                     |
| `DELETE {pattern}/{id}` | —                     | **204**                                      | **400**, **404**                     |
| `GET {pattern}/{id}`    | —                     | **200** con `RoleResponse`                   | **404**                              |
| `GET {pattern}`         | —                     | **200** con `RoleResponse[]`                 | —                                    |
| `GET {pattern}/paged`   | `?page=1&pageSize=20` | **200** con `PagedResponse<RoleResponse>`    | —                                    |

La lista sin paginar existe para poblar un desplegable, que es donde paginar estorba; la
paginada, para una pantalla de administración. Un nombre repetido llega como error con la
clave `name`.

### Usuarios

| Operación                   | Petición                  | Correcto                                     | Errores          |
| --------------------------- | ------------------------- | -------------------------------------------- | ---------------- |
| `GET {pattern}/current`     | —                         | **200** con `UserResponse`                   | **404**          |
| `GET {pattern}/{id}`        | —                         | **200** con `UserResponse`                   | **404**          |
| `GET {pattern}/paged`       | `?page=1&pageSize=20`     | **200** con `PagedResponse<UserResponse>`    | —                |
| `PUT {pattern}/{id}/status` | `UpdateUserStatusRequest` | **200** con `UserResponse`                   | **400**, **404** |
| `PUT {pattern}/{id}/roles`  | `AssignRolesRequest`      | **200** con `UserResponse`                   | **400**, **404** |

`page` es de base 1 y se acota a un mínimo de 1; `pageSize` se acota al rango `[1, 100]`,
con 20 por defecto. Un `pageSize` sin techo convertiría un endpoint de administración en una
forma de tumbar la base de datos.

Desactivar una cuenta se traduce a las columnas de bloqueo de Identity
(`LockoutEnabled` y `LockoutEnd`), no a una columna propia. Por eso **la 0.2.0 no cambia el
esquema**: no necesitas generar ni aplicar ninguna migración nueva.

### Contraseñas, correo y teléfono

Todas responden **204 No Content** cuando salen bien y **400** con
`ValidationProblemDetails` cuando no.

| Operación                      | Petición                          | Anónimo | Notas                                     |
| ------------------------------ | --------------------------------- | ------- | ----------------------------------------- |
| `POST password/change`         | `ChangePasswordRequest`           | No      | Errores bajo `newPassword`.               |
| `POST password/forgot`         | `ForgotPasswordRequest`           | Sí      | **Siempre 204**.                          |
| `POST password/reset`          | `ResetPasswordRequest`            | Sí      | Testigo inválido: **400** bajo `token`.   |
| `POST email/confirmation/send` | `SendEmailConfirmationRequest`    | Sí      | **Siempre 204**.                          |
| `POST email/confirmation`      | `ConfirmEmailRequest`             | Sí      | Testigo inválido: **400** bajo `token`.   |
| `POST email/change`            | `ChangeEmailRequest`              | No      | El aviso va al correo **nuevo**.          |
| `POST email/change/confirm`    | `ConfirmEmailChangeRequest`       | No      | Cambia `Email` y `UserName` a la vez.     |
| `POST phone/change`            | `ChangePhoneNumberRequest`        | No      | Envía el código por SMS.                  |
| `POST phone/change/confirm`    | `ConfirmPhoneNumberChangeRequest` | No      | Testigo inválido: **400** bajo `token`.   |

`password/forgot` y `email/confirmation/send` responden **204 exista o no la cuenta**.
Distinguirlo convertiría cualquiera de los dos en un verificador de qué correos tienen
cuenta, que es la misma filtración que evita el endpoint de autenticación. La consecuencia
—que el cliente no sepa si el mensaje llegó a enviarse— es deliberada: la interfaz correcta
es «si esa dirección tiene cuenta, recibirás un correo».

Los endpoints autenticados resuelven la cuenta por `ClaimTypes.Name`, igual que
`users/current`. Sin esa reclamación responden **404**.

Tampoco esta versión cambia el esquema: los testigos de Identity no se guardan, se derivan.

### Metadatos

Cada endpoint se describe a sí mismo (`Produces`, `ProducesValidationProblem`,
`WithSummary`, `WithDescription`, `WithTags("Membership")` y `AllowAnonymous`), así que si
generas un documento OpenAPI lo obtienes sin escribir nada.

Ninguno declara `401` ni `500`: el primero no llega a ocurrir porque ambos son anónimos, y
el segundo depende de los manejadores de excepciones que instales tú. Tampoco llaman a
`WithName`, porque un nombre de endpoint tiene que ser único en toda la aplicación y el
paquete montado dos veces tumbaría el arranque; si quieres nombres, ponlos sobre el
`RouteHandlerBuilder` que recibes.

## El token de acceso

Un JSON Web Token emitido con `JsonWebTokenHandler`, firmado con **HMAC-SHA256** sobre los
bytes UTF-8 de `JwtOptions.SecurityKey`.

| Reclamación         | Valor                                                    |
| ------------------- | -------------------------------------------------------- |
| `ClaimTypes.Name`   | El correo del usuario                                    |
| `Fullname`          | `"{FirstName} {LastName}"`                               |
| `ClaimTypes.Role`   | Una por cada rol del usuario. Ausente si no tiene ninguno. |
| `iss`, `aud`        | `ValidIssuer` y `ValidAudience`                          |
| `exp`, `nbf`, `iat` | Instante de emisión y `ExpireInMinutes` sobre él, en UTC |

Las reclamaciones de rol se añaden en la 0.2.0. Como `ClaimTypes.Role` es el
`RoleClaimType` que `JwtBearer` usa por defecto, `User.IsInRole("...")` y
`[Authorize(Roles = "...")]` funcionan sin configurar nada.

No lleva ninguna reclamación más; en particular, no lleva el identificador del usuario. Por
eso `GET users/current` resuelve la cuenta por el correo, que sí viaja.

`ClaimTypes.Name` es la URI `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`, y
es esa URI la que viaja literalmente en el token. Gracias a eso `User.Identity.Name`
funciona sin configurar nada, porque es el `NameClaimType` que `JwtBearer` usa por defecto.
Si esperas la reclamación corta `name`, no la vas a encontrar.

## Migraciones

El paquete **no incluye migraciones y no puede incluirlas**: son específicas del proveedor
de Entity Framework Core, y el proveedor lo eliges tú. Como `MembershipDbContext` vive en el
ensamblado del paquete, hay que decirle a Entity Framework Core que las genere en el tuyo,
con `MigrationsAssembly` como en el ejemplo de arriba. Después:

```
dotnet ef migrations add InitialMembership --context MembershipDbContext
dotnet ef database update --context MembershipDbContext
```

El esquema resultante es el estándar de ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`,
`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`,
`AspNetRoleClaims`), con `FirstName` y `LastName` añadidas a `AspNetUsers`. Las tablas de
roles existen porque las trae `IdentityDbContext`; este paquete no las usa.

## Decisiones de diseño

- **El paquete no redacta ningún mensaje.** Entrega datos y testigo por los puertos de
  salida y tú compones asunto, cuerpo y URL de vuelta.
- **Los puertos de salida no se registran solos.** Si montas un endpoint que los usa sin
  haberlos registrado, la petición falla al resolverlos. Es preferible a una
  implementación de reserva que se trague los avisos en silencio.
- **La confirmación de correo no bloquea la autenticación por sí sola.** El endpoint
  honra `IdentityOptions.SignIn.RequireConfirmedEmail` y `RequireConfirmedPhoneNumber`,
  que vienen en `false`: quien ya tenía la 0.2.0 desplegada no ve cambiar nada al
  actualizar, y quien quiera exigirlo lo activa con el `Configure<IdentityOptions>` de
  siempre.
- **El cambio de correo mueve también el nombre de usuario**, porque aquí el correo *es*
  el nombre de usuario y dejar el viejo rompería la autenticación.
- **Bloqueo por intentos fallidos.** Desde la 0.4.0 la autenticación cuenta los fallos con
  `AccessFailedAsync` y los reinicia al acertar, de modo que la política de bloqueo de
  Identity —umbral y duración— entra en juego con `Configure<IdentityOptions>`. El bloqueo
  se comprueba **antes** de la contraseña: si no, cada intento sobre una cuenta ya
  bloqueada seguiría sumando al contador y la mantendría bloqueada indefinidamente.
- **Desactivar una cuenta no apaga el interruptor de bloqueo.** Solo se mueve
  `LockoutEnd`. Apagar `LockoutEnabled` al reactivar dejaría además sin efecto el bloqueo
  por intentos fallidos, que se apoya en el mismo mecanismo.
- **La autenticación rechaza a las cuentas desactivadas.** Comprueba el bloqueo antes de
  emitir el token y responde con el mismo `Credenciales inválidas.` genérico, para no
  revelar que la cuenta existe pero está desactivada. Es un cambio de comportamiento
  respecto de la 0.1.0, y es obligado: sin él, desactivar una cuenta no impediría entrar.
- **Los roles usan el `IdentityRole` de Identity, sin tipo propio.** Un rol propio solo
  estaría justificado si el paquete añadiera columnas, y no las añade. Reutilizarlo es lo
  que permite que la 0.2.0 no toque el esquema.
- **Los endpoints de administración no fijan política de autorización.** El paquete no sabe
  qué políticas tienes, así que devuelve el `RouteHandlerBuilder` y la encadenas tú.
- **La paginación se acota en lugar de rechazarse.** Devolver un `400` por un parámetro de
  paginación es más ruidoso que útil; el techo de 100, en cambio, no es opcional.
- **El paquete emite el token; no lo valida.** `AddMembershipServices` no llama a
  `AddAuthentication` ni a `AddJwtBearer`, y los endpoints no llaman a `UseAuthentication`
  ni a `UseAuthorization`. Configurar el esquema y montar el middleware es tuyo, que eres
  quien sabe qué otros esquemas tienes y en qué orden va tu pipeline.
- **`AddIdentityCore`, nunca `AddIdentity`.** `AddIdentity` registra los esquemas de cookies
  y fija el esquema de autenticación por defecto, lo que en una aplicación que autentica con
  JWT te secuestraría la configuración.
- **Sin `SignInManager`.** La contraseña se comprueba con `UserManager.CheckPasswordAsync`:
  aquí no hay sesión que iniciar, la respuesta es un token y no una cookie. Como efecto, no
  hay bloqueo por intentos fallidos.
- **Sin dependencia de `Microsoft.AspNetCore.Authentication.JwtBearer`.** Es el paquete del
  lado que _valida_, y ese lado es tuyo.
- **La validación se ejecuta explícitamente en el manejador** con
  `Validator.TryValidateObject`, sin frameworks de terceros y sin depender de que actives
  nada en tu arranque. Ese control es lo que garantiza que el error siempre salga con la
  forma acordada.
- **Política de contraseñas por defecto** de ASP.NET Core Identity. Puedes cambiarla con
  `Configure<IdentityOptions>` después de llamar a `AddMembershipServices`.
- **Los mensajes de error no son todos del mismo idioma.** Los de las anotaciones de datos y
  los de Identity vienen del framework; el único que escribe el paquete es
  `Credenciales inválidas.`.

Quedan fuera de esta versión la renovación y revocación de tokens, los roles y las políticas
de autorización, la confirmación de correo, la recuperación y el cambio de contraseña, el
doble factor, los endpoints de perfil o de baja, los algoritmos asimétricos de firma y la
localización de los mensajes de error.

## Compatibilidad

`net10.0`.

## Estado

Versión `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

El código fuente no es público, así que esta tabla es el registro de cambios del paquete.

| Versión | Cambios                                                                                     |
| ------- | ------------------------------------------------------------------------------------------- |
| 0.5.0   | Primera versión en nuget.org: registro y autenticación sobre ASP.NET Core Identity con emisión de JWT, y los endpoints de cuenta, roles, usuarios, contraseña, correo, teléfono, perfil y doble factor. |

Las versiones `0.1.0` a `0.4.0` fueron internas y nunca llegaron a nuget.org; el texto de este
documento las menciona porque describen cuándo entró cada pieza.

## Soporte

El código fuente de este paquete no es público. Para dudas, fallos o peticiones, usa la
[página del paquete](https://aldazsoft.github.io/Membership/).

## Apoyar el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
