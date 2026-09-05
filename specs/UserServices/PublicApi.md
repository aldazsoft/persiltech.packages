---
# Paquete al que pertenece esta superficie pública. Determina el .slnx de la raíz
# y el proyecto src/<packageName>/ sobre los que se escribe el código.
packageName: Persiltech.UserServices

# MAJOR.MINOR.PATCH de la próxima publicación. Es el campo que se sube para
# preparar una nueva versión: se propaga a <VersionPrefix> del .csproj, que es
# la versión que acaba en nuget.org.
version: 0.1.4
---

# Superficie pública

## HttpContextUserService

Adaptador de infraestructura que implementa `IUserService` leyendo `HttpContext.User`
a través de `IHttpContextAccessor`. Clase `sealed`.

| Miembro                                                            | Descripción                                                                                                 |
| ------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| `HttpContextUserService(IHttpContextAccessor httpContextAccessor)` | Construye el adaptador sobre el accesor indicado. Lanza `ArgumentNullException` si es `null`.               |
| `bool IsAuthenticated { get; }`                                    | `true` solo si hay `HttpContext` activo y su `User.Identity` está autenticada.                              |
| `string? UserName { get; }`                                        | Login del usuario autenticado, o `null` si no hay usuario autenticado o ninguna reclamación aporta el dato. |
| `string? FullName { get; }`                                        | Nombre completo del usuario autenticado, o `null` si no hay usuario autenticado o no se puede componer.     |

Las propiedades se evalúan en cada lectura, nunca se cachean: el mismo objeto puede
atender peticiones distintas.

### Resolución de las reclamaciones

Cuando `IsAuthenticated` es `false`, tanto `UserName` como `FullName` son `null`; no se
leen reclamaciones de un usuario no autenticado.

`UserName` toma la primera reclamación con valor no vacío de esta lista, en orden:

1. `User.Identity.Name`, es decir, la reclamación que la propia identidad declare como
   nombre (`ClaimTypes.Name` salvo que se haya remapeado, como suele ocurrir con JWT)
2. `preferred_username` (OpenID Connect)
3. `ClaimTypes.Upn`

`FullName` toma la primera opción que produzca un valor no vacío, en orden:

1. La reclamación `name` (OpenID Connect)
2. La unión de `ClaimTypes.GivenName` y `ClaimTypes.Surname`, separadas por un espacio,
   omitiendo la que falte

Un valor formado solo por espacios en blanco cuenta como ausente y se descarta.

## DependencyInjection

Clase `static` con los métodos de extensión de `IServiceCollection` que registran el adaptador.

| Miembro                                                                          | Descripción                                                                                                                                             |
| -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IServiceCollection AddHttpContextUserService(this IServiceCollection services)` | Registra `IHttpContextAccessor` y `IUserService` → `HttpContextUserService` con tiempo de vida `Singleton`. Devuelve la misma colección para encadenar. |

El registro es idempotente: no duplica el servicio si ya estaba registrado.

# Dependencias publicadas

Versiones que viajan al `.nuspec` y que el consumidor hereda como mínimo al instalar el
paquete. Se declara la **más baja que ya expone el contrato implementado**, no la más
reciente: NuGet la trata como suelo y no como fijación, así que subirla sin motivo obliga a
actualizar a consumidores a los que la anterior les servía. Compilar contra ese mínimo es
además lo que impide usar por descuido API que solo existe en versiones posteriores.

| Paquete                                | Versión mínima | Por qué esa                                                                                                                                                    |
| -------------------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Persiltech.UserServices.Abstractions` | 0.1.0          | Primera publicada, y ya expone `IUserService` con los tres miembros que implementa el adaptador. El contrato no ha cambiado en ninguna versión posterior hasta 0.1.11. |

Cada versión de esta tabla se propaga al `<PackageVersion>` correspondiente de
`Directory.Packages.props`, que es donde Central Package Management la declara.

Las dependencias que no salen del repositorio —el arnés de pruebas y las herramientas de
compilación— no se declaran aquí: no llegan al `.nuspec`, el consumidor no las ve, y su
versión vive solo en `Directory.Packages.props`.

Subir una versión de esta tabla cambia el paquete publicado, así que lleva su propia fila en
el *Historial de versiones* del README.

# Decisiones de diseño

- El contrato `IUserService` **no se redefine aquí**: llega del paquete
  `Persiltech.UserServices.Abstractions` publicado en nuget.org, referenciado por NuGet
  vía Central Package Management. Nunca por `ProjectReference`. La versión mínima con la
  que se compila y que se declara al consumidor está en *Dependencias publicadas*.
- La dependencia de ASP.NET Core se declara con
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, no con un
  `<PackageReference>` a `Microsoft.AspNetCore.Http.Abstractions`. Ese paquete es la vía
  de ASP.NET Core 2.x sobre `netstandard2.0`: en una librería `net10.0` duplicaría tipos
  que ya trae el framework compartido. La referencia al framework también aporta
  `Microsoft.Extensions.DependencyInjection.Abstractions`, así que el paquete no declara
  ninguna dependencia NuGet más allá de las abstracciones.
- El adaptador es `sealed` y sin estado: toda la información sale de `HttpContext` en el
  momento de la lectura. Es el `IHttpContextAccessor` quien resuelve el contexto de la
  petición en curso.
- Registro `Singleton`: es el tiempo de vida que corresponde a una identidad ligada a la
  petición, y evita que un consumidor conserve por accidente valores de una petición en
  otra.
- Sin opciones de configuración para los nombres de las reclamaciones. El orden de
  resolución es fijo en esta versión; hacerlo configurable es candidato a una versión
  posterior, cuando haya un caso real que lo exija.

# Fuera de alcance

- Adaptadores para escenarios sin `HttpContext` (aplicaciones de consola, workers,
  servicios en segundo plano). Si hacen falta, corresponden a otro paquete con su propia
  solución.
- Autenticación y autorización: este paquete solo lee la identidad que el canal de
  ASP.NET Core ya haya establecido; no la establece ni la valida.
- Reclamaciones distintas de las tres propiedades del contrato (roles, permisos,
  identificadores de inquilino). Ampliar el contrato es trabajo del paquete de
  abstracciones.
