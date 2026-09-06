# Persiltech.UserServices

[![NuGet](https://img.shields.io/nuget/v/Persiltech.UserServices.svg)](https://www.nuget.org/packages/Persiltech.UserServices/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Implementación para ASP.NET Core de `IUserService` (el Output Port de
[Persiltech.UserServices.Abstractions](https://www.nuget.org/packages/Persiltech.UserServices.Abstractions/)),
que resuelve la identidad y el estado de autenticación del usuario actual a partir de
`HttpContext.User`.

## Instalación

    dotnet add package Persiltech.UserServices

## El contrato

```csharp
public sealed class HttpContextUserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    public bool IsAuthenticated { get; }
    public string? UserName { get; }
    public string? FullName { get; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddHttpContextUserService(this IServiceCollection services);
}
```

El constructor lanza `ArgumentNullException` si el accesor es `null`.

Las propiedades se evalúan en cada lectura y nunca se cachean: la misma instancia puede
atender peticiones distintas, y es el `IHttpContextAccessor` quien resuelve el contexto de
la petición en curso.

`IsAuthenticated` es `true` solo si hay `HttpContext` activo y su `User.Identity` está
autenticada. Cuando es `false`, tanto `UserName` como `FullName` son `null`: no se leen
reclamaciones de un usuario no autenticado. Por eso ambas propiedades son anulables —
el caso anónimo es legítimo, no un error.

### Resolución de las reclamaciones

`UserName` toma la primera con valor no vacío, en este orden:

1. `User.Identity.Name`, es decir, la reclamación que la propia identidad declare como
   nombre (`ClaimTypes.Name` salvo que se haya remapeado, como suele ocurrir con JWT)
2. `preferred_username` (OpenID Connect)
3. `ClaimTypes.Upn`

`FullName` toma la primera opción que produzca un valor no vacío, en este orden:

1. La reclamación `name` (OpenID Connect)
2. La unión de `ClaimTypes.GivenName` y `ClaimTypes.Surname`, separadas por un espacio,
   omitiendo la que falte

Un valor formado solo por espacios en blanco cuenta como ausente y se descarta.

## Uso

```csharp
using Persiltech.UserServices;
using Persiltech.UserServices.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextUserService();

var app = builder.Build();

app.MapGet("/whoami", (IUserService userService) => new
{
    userService.IsAuthenticated,
    userService.UserName,
    userService.FullName
});

app.Run();
```

`AddHttpContextUserService` registra `IHttpContextAccessor` y `IUserService` →
`HttpContextUserService` con tiempo de vida `Singleton`, y devuelve la misma colección para
encadenar. El registro es idempotente: llamarlo dos veces no duplica los servicios, y si la
aplicación ya tenía una implementación de `IUserService` registrada, la conserva.

El paquete no establece ni valida la autenticación: solo lee la identidad que el canal de
ASP.NET Core ya haya establecido, así que la aplicación consumidora sigue siendo la
responsable de su `UseAuthentication()` y su esquema.

## Decisiones de diseño

- El contrato `IUserService` no se redefine aquí: llega del paquete
  `Persiltech.UserServices.Abstractions` publicado en nuget.org, referenciado por NuGet.
- La dependencia de ASP.NET Core se declara con
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, no con un `<PackageReference>`
  a `Microsoft.AspNetCore.Http.Abstractions`. Ese paquete es la vía de ASP.NET Core 2.x sobre
  `netstandard2.0`: en una librería `net10.0` duplicaría tipos que ya trae el framework
  compartido. La referencia al framework también aporta
  `Microsoft.Extensions.DependencyInjection.Abstractions`, así que el paquete no declara
  ninguna dependencia NuGet más allá de las abstracciones.
- El adaptador es `sealed` y sin estado: toda la información sale de `HttpContext` en el
  momento de la lectura.
- Registro `Singleton`: es el tiempo de vida que corresponde a una identidad ligada a la
  petición, y evita que un consumidor conserve por accidente valores de una petición en otra.
- Sin opciones de configuración para los nombres de las reclamaciones. El orden de resolución
  es fijo en esta versión.

### Fuera de alcance

- Adaptadores para escenarios sin `HttpContext` (consola, workers, servicios en segundo plano).
- Autenticación y autorización.
- Reclamaciones distintas de las tres propiedades del contrato (roles, permisos,
  identificadores de inquilino): ampliar el contrato es trabajo del paquete de abstracciones.

## Compatibilidad

`net10.0`, con el framework compartido `Microsoft.AspNetCore.App`.

## Estado

Versión `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

| Versión | Cambios                                                                                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0.1.5   | La dependencia de `Persiltech.UserServices.Abstractions` sube de 0.1.0 a 0.1.12: dentro del monorepo el contrato lo aporta el proyecto vecino, y `dotnet pack` declara la versión que este tiene al empaquetar. Sin cambios en la superficie pública. |
| 0.1.4   | La dependencia mínima de `Persiltech.UserServices.Abstractions` baja de 0.1.8 a 0.1.0, la primera que ya expone el contrato implementado: instalar este paquete deja de forzar una actualización que nadie necesitaba. El README estrena este historial y retira su apartado de licencia, que nuget.org ya publica en la pestaña *License*. |
| 0.1.3   | La insignia de licencia del README enlaza al texto real y relleno de la licencia, en lugar de a la plantilla SPDX, que muestra el año y el titular sin sustituir.              |
| 0.1.2   | La página del paquete pasa a ser la del portafolio. El `.nuspec` deja de declarar el repositorio y se apaga SourceLink, porque el código fuente no es público.                 |
| 0.1.1   | La licencia MIT viaja como archivo dentro del `.nupkg` en lugar de declararse solo como expresión SPDX, así que nuget.org muestra su texto completo.                           |
| 0.1.0   | Primera publicación.                                                                                                                                                          |

La superficie pública no ha cambiado desde `0.1.0`: todo lo publicado hasta ahora corrige
empaquetado y documentación, nunca el contrato. Actualizar es siempre seguro.

## Soporte

Para dudas, informes de error o peticiones de mejora abre una [incidencia](https://github.com/aldazsoft/persiltech.packages/issues).
También puedes consultar la [página del paquete](https://aldazsoft.github.io/UserServices/).

## Apoya el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
