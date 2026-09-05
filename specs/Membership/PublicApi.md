---
# Paquete al que pertenece esta superficie pública. Determina el .slnx de la raíz
# y el proyecto src/<packageName>/ sobre los que se escribe el código.
packageName: Persiltech.Membership

# MAJOR.MINOR.PATCH de la próxima publicación. Es el campo que se sube para
# preparar una nueva versión: se propaga a <VersionPrefix> del .csproj, que es
# la versión que acaba en nuget.org.
version: 0.5.0
---

# Superficie pública

Los tipos se reparten según la tabla de _Organización de los artefactos_, al final de este
documento.

## ApplicationUser

Usuario de la aplicación. Clase `sealed` que hereda de `IdentityUser` y le añade
propiedades personalizadas. Se expone porque el consumidor la necesita para generar las
migraciones y para resolver `UserManager<ApplicationUser>` desde su propio código.

| Miembro                          | Anotaciones                      | Descripción                                                            |
| -------------------------------- | -------------------------------- | ---------------------------------------------------------------------- |
| `string FirstName { get; set; }` | `[Required]`, `[MaxLength(100)]` | Nombre del usuario. Obligatorio, hasta 100 caracteres. Nunca `null`.   |
| `string LastName { get; set; }`  | `[Required]`, `[MaxLength(100)]` | Apellido del usuario. Obligatorio, hasta 100 caracteres. Nunca `null`. |

Aquí las anotaciones no sirven para validar una petición, sino para describir la columna que genera Entity
Framework Core (`NOT NULL`, `nvarchar(100)`). Sin `MaxLength`, el proveedor las mapearía a `nvarchar(max)`.

El `UserName` y el `Email` reciben ambos el correo con el que se registró la cuenta: en
este paquete el correo _es_ el nombre de usuario.

## MembershipDbContext

Contexto de datos de Identity. Clase `sealed` que hereda de
`IdentityDbContext<ApplicationUser>` y recibe sus opciones en el constructor primario, que
las entrega tal cual a la clase base. Se expone para que el consumidor pueda generar sus
migraciones contra él.

```csharp
public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);
```

| Miembro                                                              | Descripción                                              |
| -------------------------------------------------------------------- | -------------------------------------------------------- |
| `MembershipDbContext(DbContextOptions<MembershipDbContext> options)` | Constructor primario. Es el que resuelve `AddDbContext`. |

Para el consumidor la firma del constructor es la de siempre: tanto para `AddDbContext` como para las herramientas
de `dotnet ef`. Lo resuelven igual que si estuviera escrito en el cuerpo de la clase.

No declara `DbSet<>` propios ni sobrescribe `OnModelCreating`: el modelo es el estándar de ASP.NET Core Identity más las dos columnas de `ApplicationUser`.

## JwtOptions

Opciones de emisión del token de acceso. Clase `sealed`. El consumidor las rellena con el
delegado `Action<JwtOptions>` de `AddMembershipServices`.

| Miembro                              | Anotaciones                     | Descripción                                                                      |
| ------------------------------------ | ------------------------------- | -------------------------------------------------------------------------------- |
| `string SecurityKey { get; set; }`   | `[Required]`, `[MinLength(32)]` | Clave simétrica con la que se firma el token. Obligatoria, mínimo 32 caracteres. |
| `string ValidIssuer { get; set; }`   | `[Required]`                    | Emisor que viaja en la reclamación `iss`. Obligatorio.                           |
| `string ValidAudience { get; set; }` | `[Required]`                    | Audiencia que viaja en la reclamación `aud`. Obligatoria.                        |
| `int ExpireInMinutes { get; set; }`  | `[Range(1, int.MaxValue)]`      | Minutos de vigencia del token desde su emisión. Obligatorio, mayor que cero.     |

Las anotaciones de datos se validan **al arrancar la aplicación**, no en la primera petición: `AddMembershipServices` encadena `ValidateDataAnnotations().ValidateOnStart()`. Así, una violación de restricción detiene la aplicación en el arranque, que es donde el error se ve a tiempo.

El mínimo de 32 caracteres no es arbitrario: HMAC-SHA256 exige una clave de al menos 256
bits, y una cadena de 32 caracteres ASCII es exactamente eso. Con una más corta, la
biblioteca de firma lanza una excepción al emitir el primer token, y la anotación
convierte ese fallo tardío en uno de arranque.

`SecurityKey` es un secreto: el consumidor lo aporta desde su configuración, y el paquete
no lo registra en ningún log ni lo devuelve en ninguna respuesta.

## RegisterUserRequest

Cuerpo de la petición de registro. `sealed record` con propiedades `init`.

| Miembro                            | Anotaciones                      |
| ---------------------------------- | -------------------------------- |
| `string? Email { get; init; }`     | `[Required]`, `[EmailAddress]`   |
| `string? Password { get; init; }`  | `[Required]`                     |
| `string? FirstName { get; init; }` | `[Required]`, `[MaxLength(100)]` |
| `string? LastName { get; init; }`  | `[Required]`, `[MaxLength(100)]` |

Las cuatro propiedades son **anulables y sin modificador `required`**, a propósito. Con `required`, un
cuerpo JSON al que le falte un campo falla en la deserialización, antes de que corra
ninguna validación, y el cliente recibe un error con una forma distinta de la acordada
más abajo (ver _Contratos HTTP_). Siendo anulables, en cambio, el campo ausente llega como `null`, `[Required]` lo
rechaza y el error sale como `ValidationProblemDetails`, que es el contrato.

`Password` **no lleva `[MinLength]` ni ninguna otra regla de complejidad**: la política de
contraseñas la pone ASP.NET Core Identity, y repetirla aquí daría dos mensajes distintos
para el mismo fallo.

## LoginUserRequest

Cuerpo de la petición de autenticación. `sealed record` con propiedades `init`.

| Miembro                           | Anotaciones                    |
| --------------------------------- | ------------------------------ |
| `string? Email { get; init; }`    | `[Required]`, `[EmailAddress]` |
| `string? Password { get; init; }` | `[Required]`                   |

## LoginUserResponse

Respuesta de una autenticación correcta. `sealed record` posicional.

| Miembro                                 | Descripción                                      |
| --------------------------------------- | ------------------------------------------------ |
| `LoginUserResponse(string AccessToken)` | El JWT recién emitido. Viaja como `accessToken`. |

## PagedResponse&lt;T&gt;

Página de resultados de una consulta paginada. `sealed record` posicional.

| Miembro                                                                          | Descripción                                             |
| -------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)` | Elementos de la página y los datos para situarla.       |
| `int TotalPages { get; }`                                                        | Calculada: `TotalCount` dividido entre `PageSize`, hacia arriba. |

`Page` es de base 1. `TotalPages` se calcula en lugar de recibirse para que no pueda
contradecir a los otros dos.

## RoleResponse

Rol tal como lo devuelve la API. `sealed record` posicional.

| Miembro                                 | Descripción                                     |
| --------------------------------------- | ----------------------------------------------- |
| `RoleResponse(string Id, string Name)`  | Identificador que asigna Identity y su nombre. |

## CreateRoleRequest

Cuerpo de la petición de creación de un rol. `sealed record` con propiedades `init`.

| Miembro                       | Anotaciones                     |
| ----------------------------- | ------------------------------- |
| `string? Name { get; init; }` | `[Required]`, `[MaxLength(256)]` |

El máximo de 256 caracteres es el de la columna `Name` de `AspNetRoles`.

## UpdateRoleRequest

Cuerpo de la petición de renombrado de un rol. `sealed record` con propiedades `init`.

| Miembro                       | Anotaciones                     |
| ----------------------------- | ------------------------------- |
| `string? Name { get; init; }` | `[Required]`, `[MaxLength(256)]` |

## UserResponse

Usuario tal como lo devuelve la API. `sealed record` posicional. No expone el hash de la
contraseña ni ningún otro dato de seguridad.

| Miembro                                                                                                                                    | Descripción                                                     |
| ------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| `UserResponse(string Id, string Email, string FirstName, string LastName, bool EmailConfirmed, bool IsActive, IReadOnlyList<string> Roles)` | Datos de la cuenta y los roles que tiene asignados.            |

`IsActive` es `false` cuando la cuenta está bloqueada (ver _Activación de cuentas_).

## UpdateUserStatusRequest

Cuerpo de la petición de activación o desactivación de una cuenta. `sealed record` con
propiedades `init`.

| Miembro                        | Anotaciones  |
| ------------------------------ | ------------ |
| `bool? IsActive { get; init; }` | `[Required]` |

Es anulable por la misma razón que el resto de los cuerpos de petición: un campo ausente
llega como `null` y lo rechaza `RequiredAttribute`, en lugar de fallar la deserialización.

## AssignRolesRequest

Cuerpo de la petición que fija los roles de un usuario. `sealed record` con propiedades
`init`.

| Miembro                           | Anotaciones  |
| --------------------------------- | ------------ |
| `string[]? Roles { get; init; }`  | `[Required]` |

La lista **sustituye** a la anterior: el usuario queda exactamente con los roles indicados.
Un arreglo vacío es válido y deja al usuario sin ninguno.

## IMembershipEmailSender

Puerto de salida por el que el paquete entrega al consumidor los avisos que hay que enviar
por correo. `interface` pública que **el consumidor implementa y registra**; el paquete no
trae ninguna implementación.

| Miembro                                                                                                    | Descripción                                     |
| ------------------------------------------------------------------------------------------------------------ | ----------------------------------------------- |
| `Task SendEmailConfirmationAsync(EmailConfirmationMessage message, CancellationToken cancellationToken)`   | Confirmación del correo de una cuenta nueva.   |
| `Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken)`           | Reinicio de contraseña olvidada.               |
| `Task SendEmailChangeAsync(EmailChangeMessage message, CancellationToken cancellationToken)`               | Confirmación de un cambio de correo.           |

El paquete **no redacta el mensaje**: entrega los datos y el testigo, y quien compone el
asunto, el cuerpo y la URL de vuelta es el consumidor. Redactarlo aquí obligaría al paquete
a decidir plantilla, formato e idioma, y a inventarse el patrón de ruta de la pantalla que
recibe el testigo, que es de la aplicación.

## IMembershipSmsSender

Puerto de salida equivalente para los avisos por SMS. Solo hace falta implementarlo si se
montan los endpoints de teléfono.

| Miembro                                                                                              | Descripción                            |
| ------------------------------------------------------------------------------------------------------ | -------------------------------------- |
| `Task SendPhoneChangeAsync(PhoneChangeMessage message, CancellationToken cancellationToken)`         | Confirmación de un cambio de teléfono. |

## Mensajes de los puertos de salida

Cuatro `sealed record` posicionales, uno por aviso. Todos llevan el testigo que genera
Identity, que es lo que el consumidor tiene que hacer llegar al usuario.

| Tipo                                                                                       | Descripción                                                        |
| -------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `EmailConfirmationMessage(string UserId, string Email, string FirstName, string LastName, string Token)` | Confirmación del correo con el que se registró la cuenta.        |
| `PasswordResetMessage(string UserId, string Email, string FirstName, string LastName, string Token)`     | Reinicio de contraseña.                                          |
| `EmailChangeMessage(string UserId, string NewEmail, string FirstName, string LastName, string Token)`    | Cambio de correo. `NewEmail` es el destinatario, no el actual.    |
| `PhoneChangeMessage(string UserId, string PhoneNumber, string FirstName, string LastName, string Token)` | Cambio de teléfono. El testigo es el código numérico de Identity. |

El nombre y el apellido viajan para que el consumidor pueda personalizar el saludo sin
tener que consultar la base de datos por su cuenta.

## ChangePasswordRequest

Cuerpo del cambio de contraseña de la cuenta autenticada. `sealed record` con propiedades
`init`.

| Miembro                                  | Anotaciones  |
| ---------------------------------------- | ------------ |
| `string? CurrentPassword { get; init; }` | `[Required]` |
| `string? NewPassword { get; init; }`     | `[Required]` |

## ForgotPasswordRequest

| Miembro                        | Anotaciones                    |
| ------------------------------ | ------------------------------ |
| `string? Email { get; init; }` | `[Required]`, `[EmailAddress]` |

## ResetPasswordRequest

| Miembro                              | Anotaciones                    |
| ------------------------------------ | ------------------------------ |
| `string? Email { get; init; }`       | `[Required]`, `[EmailAddress]` |
| `string? Token { get; init; }`       | `[Required]`                   |
| `string? NewPassword { get; init; }` | `[Required]`                   |

## SendEmailConfirmationRequest

| Miembro                        | Anotaciones                    |
| ------------------------------ | ------------------------------ |
| `string? Email { get; init; }` | `[Required]`, `[EmailAddress]` |

## ConfirmEmailRequest

| Miembro                        | Anotaciones                    |
| ------------------------------ | ------------------------------ |
| `string? Email { get; init; }` | `[Required]`, `[EmailAddress]` |
| `string? Token { get; init; }` | `[Required]`                   |

## ChangeEmailRequest

| Miembro                           | Anotaciones                    |
| --------------------------------- | ------------------------------ |
| `string? NewEmail { get; init; }` | `[Required]`, `[EmailAddress]` |

## ConfirmEmailChangeRequest

| Miembro                           | Anotaciones                    |
| --------------------------------- | ------------------------------ |
| `string? NewEmail { get; init; }` | `[Required]`, `[EmailAddress]` |
| `string? Token { get; init; }`    | `[Required]`                   |

## ChangePhoneNumberRequest

| Miembro                              | Anotaciones               |
| ------------------------------------ | ------------------------- |
| `string? PhoneNumber { get; init; }` | `[Required]`, `[Phone]`   |

## ConfirmPhoneNumberChangeRequest

| Miembro                              | Anotaciones             |
| ------------------------------------ | ----------------------- |
| `string? PhoneNumber { get; init; }` | `[Required]`, `[Phone]` |
| `string? Token { get; init; }`       | `[Required]`            |

## DependencyInjection

Clase `static` con el método de extensión de `IServiceCollection` que registra los servicios del
paquete.

| Miembro                                                                                                                                                                  | Descripción                                                     |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| `IServiceCollection AddMembershipServices(this IServiceCollection services, Action<JwtOptions> configureJwtOptions, Action<DbContextOptionsBuilder> configureDbContext)` | Registra Identity, el contexto de datos y la emisión de tokens. |

Lanza `ArgumentNullException` si `services` o cualquiera de los dos delegados es `null`:
sin ellos no hay ni proveedor de datos ni clave de firma, y es preferible fallar aquí que
en la primera petición.

Registra lo siguiente, en este orden:

1. `AddDbContext<MembershipDbContext>(configureDbContext)` — el proveedor de Entity
   Framework Core lo elige el consumidor dentro del delegado.
2. `AddIdentityCore<ApplicationUser>()` con las opciones por defecto, encadenado con
   `AddRoles<IdentityRole>()` y con `AddEntityFrameworkStores<MembershipDbContext>()`.
   `AddRoles` es lo que aporta `RoleManager<IdentityRole>`, que `AddIdentityCore` por sí
   solo no registra; el orden importa, porque `AddEntityFrameworkStores` tiene que ir
   después para que registre también el almacén de roles.
3. `AddOptions<JwtOptions>().Configure(configureJwtOptions).ValidateDataAnnotations().ValidateOnStart()`.
4. El emisor de tokens interno (ver _Tipos internos_).

Devuelve la misma colección para poder encadenar.

## MembershipEndpoints

Clase `static` con los métodos de extensión de `IEndpointRouteBuilder` que montan los
endpoints. El patrón de ruta sigue siendo del consumidor: el paquete propone unos por
defecto, pero no impone ninguno.

| Miembro                                                                                                                                                        | Descripción                                   |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| `IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints, string registrationPattern = "user/register", string loginPattern = "user/login")` | Monta los dos endpoints de una vez.           |
| `RouteHandlerBuilder MapUserRegistrationEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`                                                        | Monta `POST {pattern}` para crear una cuenta. |
| `RouteHandlerBuilder MapUserLoginEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`                                                               | Monta `POST {pattern}` para autenticar.       |

Hay dos caminos, y el consumidor elige según lo que necesite:

```csharp
app.MapMembershipEndpoints();                             // con las rutas por defecto
app.MapMembershipEndpoints("auth/signup", "auth/signin"); // con las suyas

app.MapUserRegistrationEndpoint("user/register").WithTags("Cuentas");
app.MapUserLoginEndpoint("user/login").RequireRateLimiting("login");
```

`MapMembershipEndpoints` no hace nada que el consumidor no pueda hacer llamando a los otros
dos: es el atajo para el caso corriente, y el sitio donde aparecerán los endpoints que
añadan las versiones siguientes. Devuelve el `IEndpointRouteBuilder` en lugar de un
`RouteHandlerBuilder` porque monta dos rutas, y ninguna de las dos representaría a la otra.

Los métodos individuales sí devuelven el `RouteHandlerBuilder`, para que el consumidor
decore cada endpoint por separado (`WithTags`, límites de tasa, lo que necesite).

Las rutas por defecto son `user/register` y `user/login`. Cambiarlas en una versión
posterior sería un cambio de contrato, no un ajuste.

### Metadatos

Cada endpoint se describe a sí mismo, de modo que el consumidor que genere un documento
OpenAPI lo obtenga sin escribir nada:

| Llamada                     | Registro                                                     | Autenticación                                             |
| --------------------------- | ------------------------------------------------------------ | ---------------------------------------------------------- |
| `Produces`                  | `201 Created`, sin cuerpo                                    | `200 OK` con `LoginUserResponse`                          |
| `ProducesValidationProblem` | `400 Bad Request`                                            | `400 Bad Request`                                          |
| `WithSummary`               | `Registrar una cuenta`                                       | `Autenticar a un usuario`                                  |
| `WithDescription`           | `Crea una cuenta a partir del correo, la contraseña y el nombre.` | `Comprueba las credenciales y devuelve un token de acceso.` |
| `WithTags`                  | `Membership`                                                 | `Membership`                                               |
| `AllowAnonymous`            | Sí                                                           | Sí                                                         |

Ninguno de los dos declara `401 Unauthorized` ni `500 Internal Server Error`. El primero
no llega a ocurrir, porque ambos endpoints son anónimos; el segundo depende de los
manejadores de excepciones que instale el consumidor, y el paquete no instala ninguno, así
que anunciarlo sería documentar algo que no controla.

Tampoco llaman a `WithName`. Un nombre de endpoint tiene que ser único en toda la
aplicación, y un paquete montado dos veces —una en `/admin` y otra en la raíz, por
ejemplo— tumbaría el arranque por un nombre repetido. El consumidor que quiera nombres los
pone sobre el `RouteHandlerBuilder` que recibe.

El `AllowAnonymous` de la tabla tampoco es decorativo: si el consumidor instala una política
de autorización de reserva, sin esa llamada el endpoint de autenticación acabaría
exigiendo el token que precisamente sirve para obtener, y nadie podría entrar.

## RoleEndpoints

Clase `static` con los métodos de extensión que montan la administración de roles. El
patrón base lo pone el consumidor; el paquete propone `roles`.

| Miembro                                                                                                       | Descripción                                          |
| --------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "roles")`      | Monta los seis endpoints de roles bajo el patrón base. |
| `RouteHandlerBuilder MapCreateRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`             | `POST {pattern}`                                     |
| `RouteHandlerBuilder MapUpdateRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`             | `PUT {pattern}` — el patrón incluye `{id}`.          |
| `RouteHandlerBuilder MapDeleteRoleEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`             | `DELETE {pattern}` — el patrón incluye `{id}`.       |
| `RouteHandlerBuilder MapGetRoleByIdEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`            | `GET {pattern}` — el patrón incluye `{id}`.          |
| `RouteHandlerBuilder MapGetRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`               | `GET {pattern}` — todos los roles, sin paginar.      |
| `RouteHandlerBuilder MapGetPagedRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`          | `GET {pattern}` — página de roles.                   |

`MapRoleEndpoints("roles")` monta `POST roles`, `PUT roles/{id}`, `DELETE roles/{id}`,
`GET roles/{id}`, `GET roles` y `GET roles/paged`.

## UserEndpoints

Clase `static` con los métodos de extensión que montan la consulta y administración de
usuarios. El patrón base lo pone el consumidor; el paquete propone `users`.

| Miembro                                                                                                        | Descripción                                     |
| ---------------------------------------------------------------------------------------------------------------- | ----------------------------------------------- |
| `IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "users")`       | Monta los cinco endpoints de usuarios.         |
| `RouteHandlerBuilder MapGetCurrentUserEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`          | `GET {pattern}` — el usuario del token.        |
| `RouteHandlerBuilder MapGetUserByIdEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`             | `GET {pattern}` — el patrón incluye `{id}`.    |
| `RouteHandlerBuilder MapGetPagedUsersEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`           | `GET {pattern}` — página de usuarios.          |
| `RouteHandlerBuilder MapUpdateUserStatusEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`        | `PUT {pattern}` — el patrón incluye `{id}`.    |
| `RouteHandlerBuilder MapAssignUserRolesEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`         | `PUT {pattern}` — el patrón incluye `{id}`.    |

`MapUserEndpoints("users")` monta `GET users/current`, `GET users/{id}`, `GET users/paged`,
`PUT users/{id}/status` y `PUT users/{id}/roles`.

## PasswordEndpoints

| Miembro                                                                                                       | Ruta con el patrón por defecto |
| --------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| `IEndpointRouteBuilder MapPasswordEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "password")` | Monta los tres.                |
| `RouteHandlerBuilder MapChangePasswordEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`          | `POST password/change`         |
| `RouteHandlerBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`          | `POST password/forgot`         |
| `RouteHandlerBuilder MapResetPasswordEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`           | `POST password/reset`          |

`forgot` y `reset` llaman a `AllowAnonymous`: quien ha olvidado su contraseña no tiene
token con el que autenticarse. `change` no, porque opera sobre la cuenta autenticada.

## EmailEndpoints

| Miembro                                                                                                          | Ruta con el patrón por defecto |
| -------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| `IEndpointRouteBuilder MapEmailEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "email")`        | Monta los cuatro.              |
| `RouteHandlerBuilder MapSendEmailConfirmationEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`     | `POST email/confirmation/send` |
| `RouteHandlerBuilder MapConfirmEmailEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`              | `POST email/confirmation`      |
| `RouteHandlerBuilder MapChangeEmailEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`               | `POST email/change`            |
| `RouteHandlerBuilder MapConfirmEmailChangeEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`        | `POST email/change/confirm`    |

Los dos de confirmación inicial son anónimos —una cuenta sin confirmar puede no poder
entrar—; los dos de cambio operan sobre la cuenta autenticada y no lo son.

## PhoneNumberEndpoints

| Miembro                                                                                                             | Ruta con el patrón por defecto |
| ----------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| `IEndpointRouteBuilder MapPhoneNumberEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "phone")`     | Monta los dos.                 |
| `RouteHandlerBuilder MapChangePhoneNumberEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`            | `POST phone/change`            |
| `RouteHandlerBuilder MapConfirmPhoneNumberChangeEndpoint(this IEndpointRouteBuilder endpoints, string pattern)`     | `POST phone/change/confirm`    |

Ambos operan sobre la cuenta autenticada.

### Metadatos de roles y usuarios

Todos declaran `WithTags("Membership")`, su `WithSummary` y su `WithDescription`, y ninguno
llama a `WithName`, por la misma razón que los dos endpoints originales.

**Ninguno llama a `AllowAnonymous` ni a `RequireAuthorization`.** Son operaciones de
administración, así que dejarlas anónimas sería un fallo de seguridad; pero el paquete no
sabe qué política tiene el consumidor —ni siquiera si ya instaló autenticación—, y fijar
una lo obligaría a llamarla igual que él. Por eso cada método devuelve su
`RouteHandlerBuilder`: la política se encadena desde fuera.

```csharp
app.MapRoleEndpoints().RequireAuthorization("Administrators");
```

Como `MapRoleEndpoints` y `MapUserEndpoints` devuelven el `IEndpointRouteBuilder`, aplicar
una política a todo el grupo de una vez exige montarlos sobre un `MapGroup` del consumidor,
que es donde encaja `RequireAuthorization` para varias rutas:

```csharp
var admin = app.MapGroup(string.Empty).RequireAuthorization("Administrators");
admin.MapRoleEndpoints();
admin.MapUserEndpoints();
```

# Contratos HTTP

Las dos respuestas de error son `ValidationProblemDetails` con **400 Bad Request**, es
decir, el objeto estándar de ASP.NET Core con su diccionario `errors`, que empareja cada
propiedad con sus mensajes.

Las claves de `errors` van en **camelCase**, igual que los campos del JSON al que se
refieren (`email`, `password`, `firstName`, `lastName`). Las anotaciones de datos entregan
el nombre del miembro en PascalCase, así que convertirlo corre por cuenta del paquete. Los
errores que no corresponden a un campo concreto usan la clave vacía `""`, que es como
ASP.NET Core representa un error de nivel de formulario.

## Registro

`POST {pattern}` con cuerpo `application/json`:

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

Errores posibles y dónde aparecen:

| Caso                                                  | Clave en `errors`                 | Origen                                                   |
| ----------------------------------------------------- | --------------------------------- | -------------------------------------------------------- |
| Campo ausente, vacío o con formato de correo inválido | el campo (`email`, `password`, …) | Anotaciones de datos                                     |
| Contraseña que no cumple la política de Identity      | `password`                        | `IdentityResult`, códigos `Password*`                    |
| Correo ya registrado                                  | `email`                           | `IdentityResult`, `DuplicateUserName` / `DuplicateEmail` |
| Cualquier otro error de Identity                      | `""`                              | `IdentityResult`                                         |

El usuario se crea con `UserName` y `Email` iguales al correo recibido, y **sin
confirmación de correo**: la cuenta queda utilizable de inmediato.

## Autenticación

`POST {pattern}` con cuerpo `application/json`:

```json
{ "email": "juan.perez@example.com", "password": "Passw0rd!" }
```

| Resultado | Respuesta                                                     |
| --------- | ------------------------------------------------------------- |
| Correcto  | **200 OK** con `{ "accessToken": "eyJhbGciOiJIUzI1NiIs..." }` |
| Error     | **400 Bad Request** con `ValidationProblemDetails`            |

| Caso                                    | Clave en `errors` | Mensaje                   |
| --------------------------------------- | ----------------- | ------------------------- |
| Campo ausente o con formato inválido    | el campo          | Anotaciones de datos      |
| Correo inexistente o contraseña errónea | `""`              | `Credenciales inválidas.` |

El correo inexistente y la contraseña errónea comparten respuesta **exacta**, y es
deliberado: distinguirlos convertiría el endpoint en un verificador de qué correos están
registrados. Por la misma razón el error va en la clave vacía y no en `email` o
`password` — señalar el campo culpable ya sería la mitad de esa filtración.

Una cuenta desactivada recibe esta misma respuesta (ver _Activación de cuentas_).

## Roles

| Operación                 | Petición                                    | Correcto                              | Errores                                             |
| ------------------------- | ------------------------------------------- | ------------------------------------- | --------------------------------------------------- |
| `POST {pattern}`          | `CreateRoleRequest`                         | **201 Created** con `RoleResponse`    | **400** con `ValidationProblemDetails`              |
| `PUT {pattern}/{id}`      | `UpdateRoleRequest`                         | **200 OK** con `RoleResponse`         | **400**, **404** si el rol no existe                |
| `DELETE {pattern}/{id}`   | —                                           | **204 No Content**                    | **400**, **404** si el rol no existe                |
| `GET {pattern}/{id}`      | —                                           | **200 OK** con `RoleResponse`         | **404** si el rol no existe                         |
| `GET {pattern}`           | —                                           | **200 OK** con `RoleResponse[]`       | —                                                   |
| `GET {pattern}/paged`     | `?page=1&pageSize=20`                       | **200 OK** con `PagedResponse<RoleResponse>` | —                                            |

El **201** de creación sí lleva cabecera `Location`, apuntando a `{pattern}/{id}`, porque
a diferencia del registro de usuarios aquí sí existe el endpoint que devuelve el recurso.

Un nombre de rol repetido llega como error de `IdentityResult` con la clave `name`. La
lista sin paginar existe para poblar un desplegable en un formulario, que es donde paginar
estorba; la paginada, para una pantalla de administración.

## Usuarios

| Operación                     | Petición                  | Correcto                                     | Errores                                    |
| ----------------------------- | ------------------------- | -------------------------------------------- | ------------------------------------------ |
| `GET {pattern}/current`       | —                         | **200 OK** con `UserResponse`                | **401** si no hay token, **404** si el usuario del token ya no existe |
| `GET {pattern}/{id}`          | —                         | **200 OK** con `UserResponse`                | **404** si el usuario no existe            |
| `GET {pattern}/paged`         | `?page=1&pageSize=20`     | **200 OK** con `PagedResponse<UserResponse>` | —                                          |
| `PUT {pattern}/{id}/status`   | `UpdateUserStatusRequest` | **200 OK** con `UserResponse`                | **400**, **404** si el usuario no existe   |
| `PUT {pattern}/{id}/roles`    | `AssignRolesRequest`      | **200 OK** con `UserResponse`                | **400** si algún rol no existe, **404** si el usuario no existe |

`GET {pattern}/current` resuelve el usuario por `ClaimTypes.Name`, que lleva su correo. Se
elige esa y no el identificador porque _El token de acceso_ deja escrito que el token no
lo lleva y que añadirlo sería un cambio de contrato; el correo, en cambio, ya viaja, y
como es a la vez el nombre de usuario identifica la cuenta igual de bien.

Devuelve **401** por la política del consumidor, no por código del paquete: si el
consumidor no exige autorización, el endpoint responde **404** al no encontrar la
reclamación.

Al fijar roles, se comprueba antes que todos existan; si alguno no, no se aplica ninguno y
la respuesta es **400** con la clave `roles`. Aplicar los válidos e ignorar el resto
dejaría al usuario en un estado que el cliente no pidió.

# El token de acceso

Un JSON Web Token emitido con `JsonWebTokenHandler`
(`Microsoft.IdentityModel.JsonWebTokens`), firmado con **HMAC-SHA256** sobre los bytes
UTF-8 de `JwtOptions.SecurityKey`.

| Reclamación         | Valor                                                    |
| ------------------- | -------------------------------------------------------- |
| `ClaimTypes.Name`   | El correo del usuario                                    |
| `Fullname`          | `"{FirstName} {LastName}"` (Ej. `"Juan Pérez"`)          |
| `ClaimTypes.Role`   | Una por cada rol del usuario. Ausente si no tiene ninguno. |
| `iss`, `aud`        | `ValidIssuer` y `ValidAudience`                          |
| `exp`, `nbf`, `iat` | Instante de emisión y `ExpireInMinutes` sobre él, en UTC |

Las reclamaciones de rol se añaden en la 0.2.0 y son la única incorporación desde la
0.1.0. Es un cambio de contrato deliberado: sin ellas los roles no servirían para
autorizar nada en el consumidor. Como `ClaimTypes.Role` es el `RoleClaimType` que
`JwtBearer` usa por defecto, `User.IsInRole("...")` y `[Authorize(Roles = "...")]`
funcionan sin configurar nada.

Cuando el usuario tiene varios roles, la reclamación viaja como un arreglo JSON bajo una
sola clave, que es como `JsonWebTokenHandler` serializa un valor múltiple.

No lleva ninguna reclamación más. En particular **no lleva el identificador del usuario**:
añadirlo sigue siendo un cambio de contrato, y por eso el endpoint del usuario actual
resuelve la cuenta por el correo (ver _Usuarios_).

`ClaimTypes.Name` es una constante cuyo valor es la URI
`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`, y es esa URI la que viaja
literalmente en el token, sin abreviar. Gracias a eso `User.Identity.Name` funciona en el
consumidor sin configurar nada, porque el `NameClaimType` que `JwtBearer` usa por defecto
es esa misma constante. Un consumidor que espere la reclamación corta `name` no la
encontrará.

# Persistencia y migraciones

El paquete **no incluye migraciones y no puede incluirlas**: son específicas del proveedor
de Entity Framework Core, y el proveedor lo elige el consumidor.

Como `MembershipDbContext` vive en el ensamblado del paquete, el consumidor tiene que
decirle a Entity Framework Core que las migraciones se generen en el suyo:

```csharp
builder.Services.AddMembershipServices(
    jwt => builder.Configuration.GetSection("Jwt").Bind(jwt),
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("Membership"),
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)));
```

Y después, desde su proyecto:

```
dotnet ef migrations add InitialMembership --context MembershipDbContext
dotnet ef database update --context MembershipDbContext
```

El esquema resultante es el estándar de ASP.NET Core Identity (`AspNetUsers`,
`AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`,
`AspNetUserTokens`, `AspNetRoleClaims`), con `FirstName` y `LastName` añadidas a
`AspNetUsers`. Las tablas de roles existen porque las trae `IdentityDbContext`; este
paquete no las usa.

# Tipos internos

No forman parte de la superficie pública, pero sí del diseño. El proyecto de pruebas los
ve gracias a `InternalsVisibleTo`.

| Tipo                                          | Papel                                                                                                      |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `internal interface IAccessTokenFactory`      | `string Create(ApplicationUser user, IReadOnlyList<string> roles)`. Aísla la emisión del token del manejador HTTP. |
| `internal sealed class JwtAccessTokenFactory` | Implementación sobre `JsonWebTokenHandler`. Depende de `IOptions<JwtOptions>`.                             |
| `internal static class RequestValidation`     | Ejecuta `Validator.TryValidateObject` y devuelve el diccionario de errores con las claves ya en camelCase. |

`JwtAccessTokenFactory` se registra como **`Singleton`**, apartándose del `Scoped` por
defecto de las convenciones: no tiene estado y su única dependencia es
`IOptions<JwtOptions>`, que también es singleton.

Nada de esto es público porque el formato del token es un detalle de implementación. Si
algún día un consumidor necesita emitir tokens por su cuenta, esa será una decisión de una
versión posterior, no un miembro que se exponga por si acaso.

# Activación de cuentas

Desactivar una cuenta se traduce a `LockoutEnabled = true` y
`LockoutEnd = DateTimeOffset.MaxValue`; activarla, a `LockoutEnd = null`. `IsActive` de
`UserResponse` es lo mismo leído al revés: `false` cuando
`UserManager.IsLockedOutAsync` devuelve `true`.

Se aprovecha el bloqueo de Identity en lugar de añadir una columna propia porque
`ApplicationUser` es `sealed` y una columna nueva obligaría a todo consumidor ya
desplegado a generar y aplicar una migración. Con esta decisión, **la versión 0.2.0 no
cambia el esquema**: las tablas de roles ya existían por herencia de `IdentityDbContext`,
y el bloqueo son columnas que Identity ya trae.

El efecto secundario es deliberado: una cuenta desactivada y una bloqueada por intentos
fallidos son indistinguibles para el paquete. Como no hay bloqueo por intentos fallidos
(no hay `SignInManager`, ver _Decisiones de diseño_), hoy el único origen posible de un
bloqueo es esta operación.

# Contratos HTTP de la 0.3.0

Todas las operaciones de contraseña, correo y teléfono responden **204 No Content** cuando
salen bien y **400 Bad Request** con `ValidationProblemDetails` cuando no.

| Operación                       | Petición                           | Anónimo | Notas                                                          |
| ------------------------------- | ---------------------------------- | ------- | -------------------------------------------------------------- |
| `POST password/change`          | `ChangePasswordRequest`            | No      | Errores de Identity bajo `newPassword` o `currentPassword`.   |
| `POST password/forgot`          | `ForgotPasswordRequest`            | Sí      | **Siempre 204** (ver abajo).                                   |
| `POST password/reset`           | `ResetPasswordRequest`             | Sí      | Testigo inválido o caducado: **400** bajo `token`.             |
| `POST email/confirmation/send`  | `SendEmailConfirmationRequest`     | Sí      | **Siempre 204**.                                               |
| `POST email/confirmation`       | `ConfirmEmailRequest`              | Sí      | Testigo inválido: **400** bajo `token`.                        |
| `POST email/change`             | `ChangeEmailRequest`               | No      | El aviso va al correo **nuevo**.                               |
| `POST email/change/confirm`     | `ConfirmEmailChangeRequest`        | No      | Cambia `Email` y `UserName` a la vez.                          |
| `POST phone/change`             | `ChangePhoneNumberRequest`         | No      | Envía el código por SMS.                                       |
| `POST phone/change/confirm`     | `ConfirmPhoneNumberChangeRequest`  | No      | Testigo inválido: **400** bajo `token`.                        |

## Por qué `forgot` y `confirmation/send` responden siempre 204

Un correo no registrado, uno ya confirmado y uno correcto reciben **exactamente la misma
respuesta**. Distinguirlos convertiría cualquiera de los dos endpoints en un verificador de
qué correos tienen cuenta, que es la misma filtración que evita el endpoint de
autenticación al no distinguir el correo inexistente de la contraseña errónea.

La consecuencia es que el cliente no puede saber si el mensaje llegó a enviarse. Es
deliberado: la interfaz correcta es «si esa dirección tiene cuenta, recibirás un correo».

## Los endpoints autenticados resuelven la cuenta por el correo

`password/change`, `email/change`, `email/change/confirm`, `phone/change` y
`phone/change/confirm` toman la cuenta de `ClaimTypes.Name`, igual que `users/current` y
por la misma razón: el token no lleva el identificador. Sin esa reclamación responden
**404**.

# Decisiones de diseño

- **El paquete no redacta ningún mensaje.** Entrega los datos y el testigo por
  `IMembershipEmailSender` / `IMembershipSmsSender` y el consumidor compone asunto, cuerpo
  y URL de vuelta. Redactar aquí obligaría al paquete a elegir plantilla, formato e idioma,
  y a inventarse el patrón de ruta de una pantalla que es de la aplicación.
- **Los puertos de salida no se registran solos.** `AddMembershipServices` no aporta
  implementación de ninguno de los dos: son del consumidor, y registrar una falsa que no
  envíe nada convertiría un olvido de configuración en un fallo silencioso. Si no se
  registran y se monta un endpoint que los usa, la petición falla al resolverlos.
- **La confirmación de correo no bloquea la autenticación por sí sola.** El endpoint de
  autenticación honra `IdentityOptions.SignIn.RequireConfirmedEmail` y
  `RequireConfirmedPhoneNumber`, que vienen en `false`. Así, quien ya tiene la 0.2.0
  desplegada no ve cambiar el comportamiento al actualizar, y quien quiera exigirlo lo
  activa con el `Configure<IdentityOptions>` de siempre, sin API propia del paquete.
- **El cambio de correo mueve también el nombre de usuario.** En este paquete el correo
  _es_ el nombre de usuario, así que confirmar el cambio actualiza `Email` y `UserName`;
  dejar `UserName` con el correo viejo rompería la autenticación.
- **El endpoint de autenticación rechaza a las cuentas desactivadas.** Comprueba
  `UserManager.IsLockedOutAsync` antes de emitir el token y, si está bloqueada, responde
  con el mismo `Credenciales inválidas.` que unas credenciales erróneas. Es un cambio de
  comportamiento respecto de la 0.1.0, y es obligado: sin él, desactivar una cuenta no
  impediría entrar, que es justo lo que la operación promete. Se responde con el error
  genérico a propósito, para no revelar a un tercero que la cuenta existe pero está
  desactivada.
- **El token de acceso lleva los roles del usuario.** Una reclamación `ClaimTypes.Role`
  por rol, obtenidas con `UserManager.GetRolesAsync` en el manejador y entregadas al
  emisor. Sin ellas los roles no servirían para autorizar nada del lado del consumidor,
  que es lo único para lo que existen. Por eso `IAccessTokenFactory.Create` cambia de
  firma; es interno, así que no afecta a la superficie pública.
- **Los roles usan el `IdentityRole` de Identity, sin tipo propio.** Un
  `ApplicationRole` propio solo estaría justificado si el paquete añadiera columnas, y no
  las añade. Reutilizarlo es lo que permite que esta versión no toque el esquema.
- **Los endpoints de administración no fijan política de autorización.** Ver _Metadatos de
  roles y usuarios_: el paquete no sabe qué políticas tiene el consumidor, así que devuelve
  el `RouteHandlerBuilder` y deja que la encadene él.
- **La paginación se valida y se acota.** `page` menor que 1 se trata como 1; `pageSize`
  fuera de `[1, 100]` se acota a ese rango. Un `pageSize` sin techo convierte un endpoint
  de administración en una forma de tumbar la base de datos, y devolver un `400` por un
  parámetro de paginación es más ruidoso que útil.
- **El paquete emite el token; no lo valida.** `AddMembershipServices` no llama a
  `AddAuthentication` ni a `AddJwtBearer`, y los endpoints no llaman a `UseAuthentication`
  ni a `UseAuthorization`. Configurar el esquema con el que se validan los tokens —y
  montar el middleware— es del consumidor, que es quien sabe qué otros esquemas tiene y en
  qué orden va su pipeline.
- **`AddIdentityCore`, nunca `AddIdentity`.** `AddIdentity` registra los esquemas de
  cookies de Identity y fija el esquema de autenticación por defecto: en una aplicación que
  autentica con JWT eso le secuestra la configuración al consumidor y rompe la decisión
  anterior. `AddIdentityCore` aporta `UserManager<ApplicationUser>` y los almacenes, que es
  todo lo que hace falta.
- **Sin `SignInManager`.** Se comprueba la contraseña con
  `UserManager.CheckPasswordAsync`. `SignInManager` arrastra la infraestructura de
  autenticación que el punto anterior deja fuera, y aquí no hay sesión que iniciar: la
  respuesta es un token, no una cookie. Como efecto, no hay bloqueo por intentos fallidos
  (ver _Fuera de alcance_).
- **Sin dependencia de `Microsoft.AspNetCore.Authentication.JwtBearer`.** Es el paquete del
  lado que _valida_, y ese lado es del consumidor. El paquete solo necesita
  `Microsoft.IdentityModel.JsonWebTokens` para firmar.
- **La dependencia de ASP.NET Core se declara con
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`**, no con `PackageReference`
  a `Microsoft.AspNetCore.*`. `Microsoft.AspNetCore.Identity.EntityFrameworkCore` y
  `Microsoft.IdentityModel.JsonWebTokens` sí son paquetes independientes y van por Central
  Package Management.
- **La validación se ejecuta explícitamente en el manejador** con
  `Validator.TryValidateObject`, sin frameworks de terceros y sin depender de que el
  consumidor active nada en su arranque. Ese control es lo que garantiza que el error
  siempre salga con la forma acordada.
- **Política de contraseñas por defecto** de ASP.NET Core Identity. El consumidor puede
  cambiarla con `Configure<IdentityOptions>` después de llamar a `AddMembershipServices`:
  el paquete no elige ninguna política, simplemente no toca la que trae Identity.
- **Los mensajes de error no son todos del mismo idioma.** Los de las anotaciones de datos
  y los de Identity vienen del framework; el único que escribe el paquete es
  `Credenciales inválidas.`. Uniformarlos exige sustituir `IdentityErrorDescriber` y
  localizar las anotaciones, y eso es del consumidor.
- **`Persiltech.UserServices` no lee `Fullname`.** Ese paquete resuelve el nombre completo
  desde la reclamación `name` o desde `ClaimTypes.GivenName` + `ClaimTypes.Surname`,
  ninguna de las cuales emite este. Los dos paquetes encajan en `UserName`, no en
  `FullName`. Es una consecuencia conocida de emitir la reclamación personalizada que pide
  la especificación, no un descuido, y conciliarlos queda como candidato para una versión
  posterior.

# Fuera de alcance

- Renovación y revocación de tokens: no hay _refresh token_, ni lista de revocación, ni
  expiración deslizante. Un token vale hasta su `exp`.
- Permisos y políticas de autorización. El paquete administra roles y los emite en el
  token; decidir qué puede hacer cada rol es del consumidor.
- Confirmación de correo, recuperación y cambio de contraseña, doble factor y bloqueo por
  intentos fallidos.
- Baja de usuarios y edición de su perfil. La cuenta se desactiva, no se borra.
- Elegir el proveedor de Entity Framework Core, generar las migraciones y aplicarlas.
- Personalizar `ApplicationUser` o `MembershipDbContext`: ambos son `sealed`, y el paquete
  no ofrece variantes genéricas del usuario ni del contexto.
- Firmar con algoritmos asimétricos (RS256 y equivalentes) y rotar claves de firma.
- Localizar los mensajes de error de Identity y de las anotaciones de datos.

# Organización de los artefactos

La raíz del proyecto contiene **lo que el consumidor nombra al componer**; el resto baja a
carpetas, y cada carpeta es un namespace anidado, según la regla de *AGENTS.md*.

| Ubicación        | Namespace                             | Contiene                                                                                                                                                    |
| ---------------- | ------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Raíz             | `Persiltech.Membership`               | `DependencyInjection`, `ApplicationUser`, `MembershipDbContext`, `JwtOptions`, `MembershipAdministrator`, `MembershipSeeder` y las ocho clases `*Endpoints`. |
| `Requests/`      | `Persiltech.Membership.Requests`      | Los cuerpos de petición.                                                                                                                                    |
| `Responses/`     | `Persiltech.Membership.Responses`     | Los cuerpos de respuesta.                                                                                                                                   |
| `Notifications/` | `Persiltech.Membership.Notifications` | Los puertos de salida y sus mensajes.                                                                                                                       |
| `Internal/`      | `Persiltech.Membership.Internal`      | Los tipos internos (ver _Tipos internos_).                                                                                                                  |

**Los métodos `Map*Endpoints` y `AddMembershipServices` se quedan en la raíz a propósito.**
Son extensiones que el consumidor invoca en su `Program.cs`, y moverlas a un namespace
anidado le obligaría a escribir un `using` por cada grupo de endpoints para montar un
paquete que se ofrece como «móntalo en una línea». Con esta distribución,
`using Persiltech.Membership;` basta para componer; los namespaces de `Requests` y
`Responses` solo hacen falta si el consumidor nombra los DTOs, cosa que no ocurre al montar
endpoints.

`Internal/` no es una carpeta decorativa: separa a simple vista el contrato de la
implementación, que es lo primero que necesita distinguir quien lee el paquete.

**Mover un tipo público de carpeta cambia su namespace, y eso es un cambio de contrato.**
Reorganizar es barato antes de la primera publicación y caro después, cuando obliga a subir
la versión mayor y a que cada consumidor edite sus `using`.
