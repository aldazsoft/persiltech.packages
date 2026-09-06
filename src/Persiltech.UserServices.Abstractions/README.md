# Persiltech.UserServices.Abstractions

[![NuGet](https://img.shields.io/nuget/v/Persiltech.UserServices.Abstractions.svg)](https://www.nuget.org/packages/Persiltech.UserServices.Abstractions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/aldazsoft/UserServices.Abstractions/blob/main/LICENSE)

Define la interfaz `IUserService`, un Output Port que expone el estado de autenticación y la identidad del usuario actual, para que cualquier solución basada en Arquitectura Limpia lo consuma.

## Instalación

    dotnet add package Persiltech.UserServices.Abstractions

## El contrato

```csharp
namespace Persiltech.UserServices.Abstractions;

public interface IUserService
{
    bool IsAuthenticated { get; }

    string? UserName { get; }

    string? FullName { get; }
}
```

`UserName` y `FullName` son anulables porque el contrato no presupone de dónde sale la identidad: un usuario anónimo no tiene ninguno de los dos, y un usuario autenticado puede carecer de nombre completo si el proveedor de identidad no emitió ese dato. Consultarlos siempre después de `IsAuthenticated` no basta para descartar el `null`, y por eso la nulabilidad es parte de la firma.

## Uso

La capa de aplicación depende del contrato, nunca de quien lo resuelve:

```csharp
namespace Persiltech.Sales.Core.PlaceOrder;

public sealed class PlaceOrderHandler(IUserService userService, IOrderRepository orderRepository)
{
    public async Task<Guid> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (!userService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        Order order = Order.Create(command.Items, userService.UserName);

        await orderRepository.AddAsync(order, cancellationToken);

        return order.Id;
    }
}
```

## Implementar el adaptador

El paquete no trae ninguna implementación: el adaptador y su registro en el contenedor corresponden a la solución consumidora, que es la que conoce su mecanismo de autenticación. En una aplicación ASP.NET Core suele resolverse desde `HttpContext.User`:

```csharp
namespace Persiltech.Sales.Api.Security;

public sealed class HttpContextUserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    private ClaimsPrincipal? CurrentUser => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => CurrentUser?.Identity?.IsAuthenticated ?? false;

    public string? UserName => CurrentUser?.FindFirstValue(ClaimTypes.Name);

    public string? FullName => CurrentUser?.FindFirstValue("full_name");
}
```

Y su registro, en el proyecto que compone la aplicación:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, HttpContextUserService>();
```

## Decisiones de diseño

- Paquete de puros contratos: no incluye ninguna implementación concreta de `IUserService` (Ej. una que la resuelva desde `HttpContext.User`); cada solución consumidora provee su propio adaptador de infraestructura para el contrato.
- Sin dependencias a `Microsoft.AspNetCore.*` ni a ningún framework de hosting, para poder referenciarse desde una capa de dominio/aplicación sin arrastrar dependencias de infraestructura.
- Sin registro en el contenedor de dependencias: no hay implementación que registrar.

## Compatibilidad

`net10.0`

## Estado

La versión es `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

El código fuente vive en el [monorepo](https://github.com/aldazsoft/persiltech.packages); esta tabla resume qué cambió en cada versión publicada.

| Versión       | Cambios                                                                                          |
| ------------- | ------------------------------------------------------------------------------------------------ |
| 0.1.14        | Restaura el historial de versiones, el soporte y el apoyo al desarrollo, que 0.1.13 perdió al migrar el paquete al monorepo. |
| 0.1.13        | El `.nuspec` declara el repositorio, ahora público, y se activa SourceLink: el depurador del consumidor puede entrar al código fuente. |
| 0.1.12        | Publica la versión que la etiqueta `v0.1.12` no llegó a subir.                                     |
| 0.1.11        | Apartado de licencia retirado; ya lo publica nuget.org.                                            |
| 0.1.10        | Historial de versiones al día en el README.                                                        |
| 0.1.9         | Insignia de licencia enlazada al texto real, no a la plantilla.                                    |
| 0.1.8         | Licencia publicada como archivo dentro del paquete.                                                |
| 0.1.7         | Metadata de empaquetado adaptada a un repositorio privado.                                         |
| 0.1.6         | Enlace absoluto al texto de la licencia en el README.                                              |
| 0.1.5         | Icono del paquete y documentación al día.                                                          |
| 0.1.4         | Documentación y metadata de empaquetado al día.                                                    |
| 0.1.0 – 0.1.3 | Primeras publicaciones de `IUserService`.                                                          |

La superficie pública no ha cambiado desde `0.1.0`: todo lo publicado hasta ahora
son cambios de documentación y de metadata del paquete.

## Soporte

Para dudas, informes de error o peticiones de mejora abre una [incidencia](https://github.com/aldazsoft/persiltech.packages/issues).
También puedes consultar la [página del paquete](https://aldazsoft.github.io/UserServices.Abstractions/).

## Apoya el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
