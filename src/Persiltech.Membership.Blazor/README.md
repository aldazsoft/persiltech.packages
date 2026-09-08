# Persiltech.Membership.Blazor

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Membership.Blazor.svg)](https://www.nuget.org/packages/Persiltech.Membership.Blazor/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/aldazsoft/persiltech.packages/blob/main/LICENSE)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Cliente Blazor de [`Persiltech.Membership`](https://www.nuget.org/packages/Persiltech.Membership/):
el estado de autenticación a partir del JWT que emite la API, la renovación automática de la
sesión con su testigo, y el manejador que firma cada petición.

## Instalación

```
dotnet add package Persiltech.Membership.Blazor
```

## El contrato

```csharp
public sealed class MembershipApiOptions
{
    [Required] public string BaseAddress { get; set; }

    public string LoginPath { get; set; } = "user/login";
    public string RegisterPath { get; set; } = "user/register";
    public string RefreshPath { get; set; } = "user/refresh";
    public string LogoutPath { get; set; } = "user/logout";
    public string UsersPath { get; set; } = "users";
    // …y una por cada grupo de endpoints del paquete servidor.
}

public sealed record MembershipTokens(string AccessToken, string RefreshToken);

public interface IMembershipTokenStore
{
    ValueTask<MembershipTokens?> GetAsync();
    ValueTask SetAsync(MembershipTokens tokens);
    ValueTask ClearAsync();
}

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

public sealed class MembershipAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync();
    public Task SignInAsync(MembershipTokens tokens);
    public Task SignOutAsync();
}

public sealed class MembershipBearerTokenHandler : DelegatingHandler;

public static class DependencyInjection
{
    public const string HttpClientName = "Persiltech.Membership";

    public static IServiceCollection AddMembershipBlazor(
        this IServiceCollection services,
        Action<MembershipApiOptions> configureOptions);
}
```

Las rutas son configurables **porque en el servidor también lo son**: los `Map*` de
`Persiltech.Membership` reciben el patrón. Si no las cambias, coinciden con las que ese
paquete propone y no configuras ninguna.

Ninguna operación lanza por un error HTTP: todas devuelven `ApiResult<T>` con el cuerpo o con
los errores por campo, para que un `400` con `ValidationProblemDetails` llegue al formulario
señalando qué está mal.

## Uso

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMembershipBlazor(options =>
    options.BaseAddress = builder.Configuration["ApiBaseAddress"]!);

builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
```

Y en una página:

```csharp
[Inject] private IMembershipApiClient Api { get; set; } = default!;
[Inject] private MembershipAuthenticationStateProvider Auth { get; set; } = default!;

private async Task SubmitAsync()
{
    var result = await Api.LoginAsync(new LoginUserRequest(Email, Password));

    if (!result.Succeeded || result.Value is null)
    {
        Errors = result.Errors;
        return;
    }

    await Auth.SignInAsync(result.Value);
}
```

Para llamar a los endpoints que esta versión todavía no cubre, resuelve el mismo `HttpClient` con
nombre: ya lleva la dirección base y el manejador que firma.

```csharp
var http = factory.CreateClient(DependencyInjection.HttpClientName);
```

## Los formularios

Cuatro componentes de MudBlazor que hacen la llamada, pintan los errores por campo que
devuelva la API y avisan por un `EventCallback`:

| Componente                     | Aviso                                |
| ------------------------------ | ------------------------------------ |
| `MembershipLoginForm`          | `OnLoggedIn` (`MembershipTokens`)    |
| `MembershipRegisterForm`       | `OnRegistered` (`string`, el correo) |
| `MembershipForgotPasswordForm` | `OnRequested` (`string`, el correo)  |
| `MembershipResetPasswordForm`  | `OnReset`                            |

**No llevan `@page`.** Un paquete que fijara las rutas chocaría con las tuyas y te quitaría el
control de la navegación y de la maquetación. Lo que se reutiliza es el formulario; tú lo
envuelves en la página que quieras:

```razor
@page "/login"

<MudCard Outlined="true" Style="max-width: 520px">
    <MudCardContent>
        <MembershipLoginForm InitialEmail="@Email" OnLoggedIn="OnLoggedInAsync" />
    </MudCardContent>
</MudCard>

@code {
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private Task OnLoggedInAsync(MembershipTokens tokens)
    {
        Navigation.NavigateTo("profile");

        return Task.CompletedTask;
    }
}
```

`MembershipLoginForm` **abre la sesión por su cuenta** —llama a `SignInAsync` antes de invocar
`OnLoggedIn`—, porque si no, cada aplicación repetiría el mismo par de líneas y una de ellas se
olvidaría. A dónde ir después sí es tuyo.

El campo del segundo factor **solo aparece cuando la API lo pide**: mostrarlo siempre
invitaría a rellenarlo a quien no lo tiene activado.

`MembershipRegisterForm` no abre la sesión: según tu política, la cuenta puede necesitar
confirmar el correo antes de poder entrar.

Los cuatro aceptan `SubmitLabel` para el texto del botón y `Dense` para encajar en un diálogo.
Para pintar los errores de una llamada tuya, `MembershipValidationErrors` toma el
`ApiResult<T>.Errors` directamente.

## La sesión

**El token se lee, no se valida.** La firma la comprueba la API en cada petición; hacerlo
también en el navegador no añadiría seguridad —el cliente está en manos de quien lo usa— pero
sí obligaría a repartir la clave.

**Un token caducado no cierra la sesión: la renueva.** Si hay testigo de renovación,
`GetAuthenticationStateAsync` lo cambia por un par nuevo antes de darse por vencido. Solo si
la renovación falla queda anónimo.

**Cerrar sesión avisa primero a la API**, para que revoque la familia del testigo. Borrarlo
solo del navegador lo dejaría vivo para quien lo hubiera copiado.

## Dónde viven los testigos

`IMembershipTokenStore` es una interfaz porque **dónde se guarda un testigo es una decisión de
seguridad tuya**. La implementación por defecto usa `localStorage`, que es lo único que
funciona sin un backend propio, pero sobrevive al cierre de la pestaña y lo lee cualquier
script de la página: un XSS lo expone. Si tienes backend, guarda el de renovación en una
cookie `HttpOnly` e implementa esta interfaz a tu manera.

La implementación por defecto **no cachea en memoria**. Es deliberado:
`IHttpClientFactory` resuelve los `DelegatingHandler` en su propio ámbito, así que el
manejador que firma recibe una instancia distinta de la que usa la interfaz. Con caché, esa
instancia aprendería «no hay sesión» en la primera petición —la de autenticarse— y todo lo
posterior saldría sin firmar.

## Decisiones de diseño

- **MudBlazor es la biblioteca de interfaz**, no una opción.
- **Los contratos se redeclaran** en lugar de referenciar `Persiltech.Membership`: ese es un
  paquete de servidor y arrastra Identity y Entity Framework Core, que no pintan nada en el
  navegador.
- **El manejador que firma se engancha solo al cliente con nombre del paquete.** Uno global
  mandaría tu token a cualquier dominio al que la aplicación llamara.
- **Las opciones se comprueban al registrar**, no con `ValidateOnStart`: en WebAssembly no hay
  host que arranque servicios, así que esa validación nunca correría.

## Compatibilidad

`net10.0`, sobre Blazor WebAssembly.

## Estado

Preliberación: la superficie pública puede cambiar antes de la 2.0.0 definitiva.

Esta preliberación trae el núcleo de la sesión y sus formularios. Lo que llega después:
perfil, cambio de contraseña, correo, teléfono y doble factor; y luego roles y usuarios.

## Historial de versiones

El código fuente vive en el [monorepo](https://github.com/aldazsoft/persiltech.packages); esta
tabla resume qué cambió en cada versión publicada.

| Versión           | Cambios                                                                                     |
| ----------------- | ------------------------------------------------------------------------------------------- |
| 2.0.0-preview.1   | **Reescritura completa.** Cliente de `Persiltech.Membership` 0.6.0: estado de autenticación con renovación, almacén de testigos sustituible, manejador que firma, y los formularios de sesión, registro y contraseña. |
| 1.0.0 – 1.0.1     | Versiones del monorepo anterior, con otra API y con las pantallas de empleados y clientes.  |

**El salto de la 1.0.1 a la 2.0.0 no es una actualización: es otro paquete bajo el mismo
nombre.** Cambian los espacios de nombres, los tipos y la composición, y desaparecen las
pantallas de empleados y clientes, que eran dominio de la aplicación y no de la membresía.
Migrar desde la 1.0.1 es reescribir la integración, no subir una versión.

El sufijo `-preview` dice que la superficie todavía se mueve: faltan las pantallas de perfil,
roles y usuarios.

## Soporte

Para dudas, fallos o peticiones abre una [incidencia](https://github.com/aldazsoft/persiltech.packages/issues).
También puedes consultar la [página del paquete](https://aldazsoft.github.io/Membership.Blazor/).

## Apoyar el desarrollo

Si este paquete te resulta útil, puedes [patrocinar su desarrollo](https://github.com/sponsors/aldazsoft).
