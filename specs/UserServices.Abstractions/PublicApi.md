---
# Paquete al que pertenece esta superficie pública. Determina el .slnx de la raíz
# y el proyecto src/<packageName>/ sobre los que se escribe el código.
packageName: Persiltech.UserServices.Abstractions

# MAJOR.MINOR.PATCH de la próxima publicación. Es el campo que se sube para
# preparar una nueva versión: se propaga a <VersionPrefix> del .csproj, que es
# la versión que acaba en nuget.org.
version: 0.1.12
---

# Superficie pública

## IUserService

Output Port que expone la identidad y el estado de autenticación del usuario actual.

| Miembro                         | Descripción                                                                            |
| ------------------------------- | -------------------------------------------------------------------------------------- |
| `bool IsAuthenticated { get; }` | Indica si el usuario actual está autenticado.                                          |
| `string? UserName { get; }`     | Nombre de usuario (login) del usuario actual, o `null` si no se dispone de dicho dato. |
| `string? FullName { get; }`     | Nombre completo del usuario actual, o `null` si no se dispone de dicho dato.           |

# Decisiones de diseño

- Paquete de puros contratos: no incluye ninguna implementación concreta de `IUserService` (Ej. una que la resuelva desde `HttpContext.User`); cada solución consumidora provee su propio adaptador de infraestructura para el contrato.
- Sin dependencias a `Microsoft.AspNetCore.*` ni a ningún framework de hosting, para poder referenciarse desde una capa de dominio/aplicación sin arrastrar dependencias de infraestructura.
- Sin registro en el contenedor de dependencias: no hay implementación que registrar.

# Fuera de alcance

- Las soluciones que ya definían `IUserService` localmente, deberán reemplazar esa definición por una referencia a este paquete; ese reemplazo no forma parte de este paquete.
