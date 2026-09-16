---
# Paquete al que pertenece esta superficie pública.
packageName: Persiltech.Membership.Blazor

# MAJOR.MINOR.PATCH de la próxima publicación. El sufijo de preliberación vive solo en el
# .csproj (<VersionSuffix>), que es la fuente de verdad de lo que se publica.
version: 2.0.0
---

# Superficie pública

La 2.0.0-preview.1 cubre **el núcleo reutilizable** —la sesión y el acceso a la API— y los
**formularios de sesión** que se montan sobre él. Las pantallas de perfil y de
administración llegan después (ver _Hoja de ruta_).

## Formularios, no páginas

Los componentes que expone el paquete **no llevan `@page`**. Un paquete que fijara las rutas
chocaría con las del consumidor y le quitaría el control de la navegación y de la
maquetación: quien instala esto ya tiene su propio menú, su propio diseño y sus propias
direcciones. Lo que se reutiliza es el formulario —los campos, la validación, la llamada y el
tratamiento del error—, y el consumidor lo envuelve en la página que quiera.

Cada formulario avisa de lo que ha conseguido con un `EventCallback`, en lugar de navegar por
su cuenta: a dónde ir después de entrar es una decisión de la aplicación.

## MembershipApiOptions

Dónde está la API y en qué rutas montó sus endpoints. Clase `sealed`.

| Miembro                                       | Descripción                                                    |
| --------------------------------------------- | -------------------------------------------------------------- |
| `string BaseAddress { get; set; }`            | Raíz de la API. Obligatoria.                                   |
| `string LoginPath { get; set; }`              | Por defecto `user/login`.                                      |
| `string RegisterPath { get; set; }`           | Por defecto `user/register`.                                   |
| `string RefreshPath { get; set; }`            | Por defecto `user/refresh`.                                    |
| `string LogoutPath { get; set; }`             | Por defecto `user/logout`.                                     |
| `string PasswordPath { get; set; }`           | Base del grupo. Por defecto `password`.                        |
| `string EmailPath { get; set; }`              | Base del grupo. Por defecto `email`.                           |
| `string PhonePath { get; set; }`              | Base del grupo. Por defecto `phone`.                           |
| `string ProfilePath { get; set; }`            | Base del grupo. Por defecto `profile`.                         |
| `string TwoFactorPath { get; set; }`          | Base del grupo. Por defecto `twofactor`.                       |
| `string RolesPath { get; set; }`              | Base del grupo. Por defecto `roles`.                           |
| `string UsersPath { get; set; }`              | Base del grupo. Por defecto `users`.                           |

Las rutas son configurables **porque en el servidor también lo son**: los `Map*` de
`Persiltech.Membership` reciben el patrón, así que un cliente que las fijara obligaría a
montar la API en las suyas. Los valores por defecto coinciden con los del paquete servidor,
de modo que quien no las cambie no configura ninguna.

## MembershipTokens

Par de testigos de una sesión. `sealed record` posicional.

| Miembro                                              | Descripción                              |
| ---------------------------------------------------- | ---------------------------------------- |
| `MembershipTokens(string AccessToken, string RefreshToken)` | Lo que devuelve el inicio de sesión. |

## IMembershipTokenStore

Dónde viven los testigos entre peticiones. El paquete trae una implementación sobre
`localStorage` y el consumidor puede sustituirla.

```csharp
public interface IMembershipTokenStore
{
    ValueTask<MembershipTokens?> GetAsync();
    ValueTask SetAsync(MembershipTokens tokens);
    ValueTask ClearAsync();
}
```

Es una interfaz **porque dónde se guarda un testigo es una decisión de seguridad del
consumidor**, no del paquete: `localStorage` sobrevive al cierre de la pestaña y lo lee
cualquier script de la página, así que un XSS lo expone. La implementación por defecto lo
usa por ser lo que funciona sin backend propio; quien tenga uno puede guardar el de
renovación en una cookie `HttpOnly` y quedarse aquí solo con el de acceso.

## IMembershipApiClient

Una operación por endpoint que publica `Persiltech.Membership`. Devuelve
`ApiResult<T>`, nunca lanza por un error HTTP.

```csharp
public interface IMembershipApiClient
{
    Task<ApiResult<MembershipTokens>> LoginAsync(LoginUserRequest request);
    Task<ApiResult<Unit>> RegisterAsync(RegisterUserRequest request);
    Task<ApiResult<MembershipTokens>> RefreshAsync(string refreshToken);
    Task<ApiResult<Unit>> LogoutAsync(string refreshToken);
    Task<ApiResult<UserResponse>> GetCurrentUserAsync();
    Task<ApiResult<Unit>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResult<Unit>> ResetPasswordAsync(ResetPasswordRequest request);
}
```

Son las que necesitan la sesión y sus formularios. El resto de grupos entra con sus
pantallas.

## Los formularios

Todos son `sealed`, viven en `Components/` y comparten forma: un `EditForm` sobre un modelo
interno, campos de MudBlazor con `For`, la llamada al cliente y el aviso por su `EventCallback`.

| Componente                       | Qué hace                                        | Aviso                                  |
| -------------------------------- | ----------------------------------------------- | -------------------------------------- |
| `MembershipLoginForm`            | Autentica y abre la sesión.                     | `OnLoggedIn` (`MembershipTokens`)      |
| `MembershipRegisterForm`         | Crea una cuenta.                                | `OnRegistered` (`string`, el correo)   |
| `MembershipForgotPasswordForm`   | Pide el correo de reinicio.                     | `OnRequested` (`string`, el correo)    |
| `MembershipResetPasswordForm`    | Fija la contraseña con el testigo recibido.     | `OnReset`                              |

## Dónde aterrizan los errores

Cada formulario lleva un `DataAnnotationsValidator` y un `ApiValidator`, de
`Persiltech.Validation.Blazor`. Los dos escriben en el mismo `EditContext`, así que el mensaje
se pinta bajo su campo venga del navegador o del servidor.

El modelo de cada formulario es `internal` y sus propiedades **se llaman como las claves que
serializa la API** —`email`, `password`, `twoFactorCode`, `firstName`, `lastName`,
`newPassword`—: ese emparejamiento es lo que lleva cada error a su campo. Renombrar una
propiedad sin renombrar la clave deja su mensaje sin sitio donde pintarse.

Lo que no pertenece a ningún campo sale en un aviso encima del formulario. Ahí cae el error
general, y en `MembershipResetPasswordForm` también el del testigo caducado o inválido, que no
tiene campo porque el testigo no se muestra.

`ConfirmPassword` solo existe en el navegador: el servidor recibe una sola contraseña. Su aviso
lo produce `ResetPasswordValidation` con la misma forma que `ValidationProblemDetails` y entra
por `ApiValidator.Show`, de modo que aterriza bajo su campo como cualquier otro.

Parámetros comunes a los cuatro formularios:

| Parámetro                          | Descripción                                                  |
| ---------------------------------- | ------------------------------------------------------------ |
| `string SubmitLabel { get; set; }` | Texto del botón. Cada uno trae el suyo por defecto.          |
| `bool Dense { get; set; }`         | Campos compactos, para encajar en un diálogo.                |

**`MembershipLoginForm` abre la sesión por su cuenta**: llama a
`MembershipAuthenticationStateProvider.SignInAsync` antes de invocar `OnLoggedIn`. Es lo único
que no puede quedar en manos del consumidor sin que el formulario deje de servir para nada —
si no, cada aplicación repetiría el mismo par de líneas y una de ellas se olvidaría.

El campo del segundo factor **solo aparece cuando la API lo pide**: la primera respuesta con
el error en `twoFactorCode` lo revela. Mostrarlo siempre invitaría a rellenarlo a quien no lo
tiene activado.

## ApiResult&lt;T&gt;

Respuesta de la API ya interpretada. `sealed class`.

| Miembro                                                    | Descripción                                          |
| ---------------------------------------------------------- | ---------------------------------------------------- |
| `T? Value { get; }`                                        | El cuerpo, si la llamada fue bien.                   |
| `IReadOnlyDictionary<string, string[]> Errors { get; }`    | Los errores de `ValidationProblemDetails`.           |
| `HttpStatusCode StatusCode { get; }`                       | El código que devolvió la API.                       |
| `bool Succeeded { get; }`                                  | Calculada: no hay errores.                           |
| `string ErrorSummary { get; }`                             | Todos los mensajes en una línea, para un aviso.      |

Existe **para que un 400 con `ValidationProblemDetails` llegue a la pantalla con sus campos**
en lugar de como una excepción sin contexto. Es el punto único por el que pasa toda llamada.

## MembershipAuthenticationStateProvider

Deriva el estado de autenticación de las reclamaciones del token.

```csharp
public sealed class MembershipAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync();
    public Task SignInAsync(MembershipTokens tokens);
    public Task SignOutAsync();
}
```

**El token se lee, no se valida.** La firma la comprueba la API en cada petición; hacerlo
también aquí no añadiría seguridad —el cliente está en manos de quien lo usa— pero sí
obligaría a repartir la clave.

**Un token caducado no cierra la sesión: la renueva.** Si hay testigo de renovación,
`GetAuthenticationStateAsync` lo cambia por un par nuevo antes de darse por vencido. Solo si
la renovación falla queda anónimo. Es lo que hace que la sesión sobreviva al `exp` sin pedir
la contraseña otra vez.

`SignOutAsync` avisa primero a la API para que revoque la familia del testigo: borrarlo solo
del navegador lo dejaría vivo para quien lo hubiera copiado.

## MembershipBearerTokenHandler

`DelegatingHandler` que firma cada petición saliente con el token de acceso.

Se registra sobre el `HttpClient` con nombre del paquete, no sobre el del consumidor: un
manejador global mandaría el token a cualquier dominio al que la aplicación llamara.

## DependencyInjection

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddMembershipBlazor(
        this IServiceCollection services,
        Action<MembershipApiOptions> configureOptions);
}
```

Registra las opciones, el `HttpClient` con nombre y su manejador, el cliente de la API, el
almacén de testigos por defecto y el proveedor de estado de autenticación.

No llama a `AddMudServices`: MudBlazor es del consumidor, que ya lo registra para su propia
interfaz, y hacerlo dos veces duplicaría sus proveedores.

# Contratos

El paquete **redeclara** los cuerpos de petición y respuesta de la API en lugar de
referenciar `Persiltech.Membership`.

No es duplicación por descuido. `Persiltech.Membership` es un paquete de servidor: arrastra
ASP.NET Core Identity y Entity Framework Core, y referenciarlo desde una biblioteca que
acaba en el navegador metería todo eso en el `.wasm`. Un cliente de una API solo conoce el
JSON que esa API publica, y copiarlo es además lo que pone a prueba que el contrato baste.

La alternativa —extraer un `Persiltech.Membership.Contracts` que compartan los dos— es
mejor a largo plazo y está anotada en la _Hoja de ruta_; hacerla ahora obligaría a publicar
una versión mayor del paquete servidor, que acaba de salir.

# Decisiones de diseño

- **MudBlazor es la biblioteca de interfaz**, no una opción. Es lo que usan las dos
  aplicaciones de la casa y lo que usaba el paquete anterior; ofrecer componentes sin estilo
  duplicaría el trabajo para un consumidor que no existe.
- **El SDK es `Microsoft.NET.Sdk.Razor` y no se declara `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.**
  Una Razor Class Library que lo declare rompe a las aplicaciones Blazor WebAssembly, que no
  referencian ese framework compartido. Es el mismo criterio que `Persiltech.Blazor.JSInterop`.
- **Las rutas de la API son configuración**, porque en el servidor también lo son.
- **El almacén de testigos es una interfaz**, porque dónde vive un testigo es una decisión
  de seguridad del consumidor.

# Fuera de alcance

- **El dominio de empleados y clientes.** El paquete anterior traía altas, ediciones y
  listados con documento de identidad, género, nacionalidad y fecha de nacimiento. Eso no es
  membresía: es el dominio de la aplicación que la usa, y su sitio es esa aplicación. El
  paquete servidor tampoco lo conoce.
- **Login con proveedor externo** (Google, Facebook). `Persiltech.Membership` todavía no lo
  emite, así que aquí no hay nada que consumir.
- **Tema, cajón lateral y diálogos genéricos.** Son de la aplicación, no de la membresía.
- **Localización de los mensajes.** Los textos viajan en español, como el resto de la casa.

# Hoja de ruta

| Versión | Qué entra |
| ------- | --------- |
| 0.1.0   | El núcleo —opciones, cliente de API, `ApiResult<T>`, almacén de testigos, estado de autenticación con renovación, el manejador que firma— y los formularios de sesión: entrar, registrarse, contraseña olvidada y reinicio. |
| 0.2.0   | Perfil, cambio de contraseña, cambio y confirmación de correo, cambio de teléfono, doble factor. |
| 0.3.0   | Administración: roles y usuarios. |

Los formularios de sesión estaban previstos para una 0.2.0, pero la 0.1.0 no llegó a
publicarse: separarlos habría dejado en el historial una versión que nadie pudo instalar.

Extraer `Persiltech.Membership.Contracts` queda anotado como candidato para cuando el
paquete servidor suba de mayor.

# Organización de los artefactos

| Ubicación     | Namespace                                   | Contiene                                                      |
| ------------- | ------------------------------------------- | ------------------------------------------------------------- |
| Raíz          | `Persiltech.Membership.Blazor`              | `DependencyInjection`, `MembershipApiOptions` y los tipos que el consumidor nombra al componer. |
| `Components/` | `Persiltech.Membership.Blazor.Components`   | Los formularios y el componente de errores.                   |
| `Contracts/`  | `Persiltech.Membership.Blazor.Contracts`    | Los cuerpos de petición y respuesta y `ApiResult<T>`.         |
| `Services/`   | `Persiltech.Membership.Blazor.Services`     | El cliente de la API, el almacén de testigos y el estado de autenticación. |
| `Internal/`   | `Persiltech.Membership.Blazor.Internal`     | Lo que no forma parte del contrato.                           |
