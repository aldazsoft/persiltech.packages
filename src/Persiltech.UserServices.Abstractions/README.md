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
