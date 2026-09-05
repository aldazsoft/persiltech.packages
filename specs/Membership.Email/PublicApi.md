---
# Paquete al que pertenece esta superficie pública. Determina el .slnx de la raíz
# y el proyecto src/<packageName>/ sobre los que se escribe el código.
packageName: Persiltech.Membership.Email

# MAJOR.MINOR.PATCH de la próxima publicación. Es el campo que se sube para
# preparar una nueva versión: se propaga a <VersionPrefix> del .csproj, que es
# la versión que acaba en nuget.org.
version: 0.1.0
---

# Superficie pública

El paquete implementa `IMembershipEmailSender` —el puerto de salida de
`Persiltech.Membership`— redactando cada aviso a partir de plantillas HTML y entregándolo
por `IEmailSender`, el puerto de transporte de `Persiltech.Email`.

Es la mitad que `Persiltech.Membership` deja fuera a propósito: la que conoce la marca, el
diseño y las rutas de la aplicación. Se hace reutilizable dejando esas tres cosas en
`Options` y en plantillas sustituibles, no en el código.

Todos los tipos públicos viven en la raíz del proyecto, en el namespace
`Persiltech.Membership.Email`.

## MembershipEmailOptions

Marca, rutas de la aplicación cliente y origen de las plantillas. Clase `sealed`, que el
consumidor rellena con el delegado `Action<MembershipEmailOptions>` de `AddMembershipEmail`.

| Miembro                                      | Descripción                                                                                     |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `string BrandName { get; set; }`             | Nombre de la marca, en el encabezado, el pie y los asuntos. Obligatorio.                        |
| `string ClientBaseUrl { get; set; }`         | Raíz de la aplicación **cliente**, no de la API. Obligatoria, y URL absoluta http o https.      |
| `string EmailConfirmationPath { get; set; }` | Ruta de la pantalla que confirma el correo. Por defecto `/confirm-email`. No puede quedar vacía. |
| `string PasswordResetPath { get; set; }`     | Ruta de la pantalla que reinicia la contraseña. Por defecto `/reset-password`. No puede quedar vacía. |
| `string EmailChangePath { get; set; }`       | Ruta de la pantalla que confirma el cambio de correo. Por defecto `/confirm-email-change`. No puede quedar vacía. |
| `string? LogoUrl { get; set; }`              | Logotipo del encabezado. Opcional; si se indica, URL absoluta. Sin él se rotula la marca como texto. |
| `string PrimaryColor { get; set; }`          | Color del encabezado y del botón, en hexadecimal (`#rgb` o `#rrggbb`). Por defecto `#0d6efd`.   |
| `string? SupportEmail { get; set; }`         | Correo de contacto del pie. Opcional; si se indica, tiene que ser una dirección válida.         |
| `string? TemplatesDirectory { get; set; }`   | Directorio en disco cuyas plantillas ganan a las embebidas, por nombre de archivo. Si se indica, tiene que existir. |

Las reglas se comprueban **al arrancar la aplicación**: `AddMembershipEmail` registra un
`IValidateOptions<MembershipEmailOptions>` y encadena `ValidateOnStart()`. El validador
**acumula** todos los fallos en una sola respuesta.

Que `TemplatesDirectory` tenga que existir no es celo: apuntando a un directorio que no
está, se servirían las plantillas embebidas y nadie se enteraría de que el rebrandeo no se
aplicó. Es el fallo silencioso que la validación convierte en uno de arranque.

`LogoUrl` tiene que ser absoluta porque los clientes de correo no resuelven rutas relativas.

`ClientBaseUrl` es la raíz de la aplicación que abre el usuario, y de ella cuelgan las tres
rutas. El paquete la usa tal cual, sin barra final duplicada.

`TemplatesDirectory` es lo que permite rebrandear sin bifurcar el paquete: basta con dejar
en esa carpeta un archivo con el mismo nombre que el embebido. Las plantillas se leen una
vez y se cachean, así que un cambio en disco exige reiniciar la aplicación.

## IEmailTemplateRenderer

Compone el asunto y los dos cuerpos de un aviso. Es público para que un consumidor pueda
sustituir la sustitución de marcadores por un motor con condicionales o bucles (Scriban,
Fluid) sin tocar el adaptador.

| Miembro                                                                                  | Descripción                                        |
| ---------------------------------------------------------------------------------------- | -------------------------------------------------- |
| `RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string?> values)` | Compone el aviso con los valores indicados.        |

Se registra con `TryAddSingleton`, de modo que una implementación propia registrada antes de
`AddMembershipEmail` gana.

## RenderedEmail

Resultado de componer un aviso. `sealed record` posicional.

```csharp
public sealed record RenderedEmail(string Subject, string HtmlBody, string TextBody);
```

| Miembro            | Descripción                                                        |
| ------------------ | ------------------------------------------------------------------ |
| `string Subject`   | Asunto ya sustituido.                                              |
| `string HtmlBody`  | Cuerpo HTML completo: el aviso ya envuelto en el diseño común.    |
| `string TextBody`  | Cuerpo alternativo en texto plano.                                 |

El texto plano no es opcional: un correo transaccional sin parte de texto pierde puntos de
reputación y se ve mal en los clientes que no muestran HTML.

## DependencyInjection

Registro en el contenedor. Clase `static` en la raíz del proyecto.

| Miembro                                                                                                                     | Descripción                                                        |
| --------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `static IServiceCollection AddMembershipEmail(this IServiceCollection services, Action<MembershipEmailOptions> configureOptions)` | Registra `IMembershipEmailSender` y el compositor de plantillas.   |

Devuelve la misma colección, para poder encadenar. Lanza `ArgumentNullException` si falta
cualquiera de los dos argumentos.

Deja registrados exactamente estos tres servicios:

| Servicio                                     | Implementación                    | Tiempo de vida | Cómo              |
| -------------------------------------------- | --------------------------------- | -------------- | ----------------- |
| `IValidateOptions<MembershipEmailOptions>`   | `MembershipEmailOptionsValidator` | `Singleton`    | `TryAddSingleton` |
| `IEmailTemplateRenderer`                     | `EmbeddedTemplateRenderer`        | `Singleton`    | `TryAddSingleton` |
| `IMembershipEmailSender`                     | `TemplatedMembershipEmailSender`  | `Scoped`       | `AddScoped`       |

Más las `IOptions<MembershipEmailOptions>` que aporta
`AddOptions().Configure().ValidateOnStart()`.

**No registra el transporte.** `IEmailSender` lo aporta el consumidor —normalmente con
`AddSmtpEmailSender` de `Persiltech.Email`—, porque elegir servidor y credenciales es suyo.
El orden entre ambas llamadas da igual; lo que no puede es faltar una.

# Las plantillas

Cada aviso son tres archivos, embebidos en el ensamblado bajo `Templates/`:

| Aviso                 | Nombre             | Archivos                                                                   |
| --------------------- | ------------------ | -------------------------------------------------------------------------- |
| Confirmación de correo | `EmailConfirmation` | `EmailConfirmation.subject.txt`, `.html`, `.txt`                          |
| Reinicio de contraseña | `PasswordReset`     | `PasswordReset.subject.txt`, `.html`, `.txt`                              |
| Cambio de correo       | `EmailChange`       | `EmailChange.subject.txt`, `.html`, `.txt`                                |

El `.html` de cada aviso es **solo el interior**: el encabezado, el ancho de 600 píxeles y el
pie viven una única vez en `Layout.html`, que envuelve a los tres.

## Marcadores

La sintaxis es `{{Nombre}}`. Un marcador que no corresponda a ningún valor **lanza**
`InvalidOperationException` nombrándolo: una plantilla escrita a mano con un nombre mal
tecleado tiene que fallar en el primer envío de prueba, no llegar en blanco al buzón de un
cliente.

| Marcador                                    | Origen                                                             |
| ------------------------------------------- | ------------------------------------------------------------------ |
| `FirstName`, `LastName`, `FullName`         | El aviso.                                                          |
| `Email`                                     | Destinatario del aviso.                                            |
| `ActionUrl`                                 | Enlace de vuelta a la aplicación cliente, ya construido.           |
| `BrandName`, `PrimaryColor`, `SupportEmail` | Las opciones.                                                      |
| `BrandHeader`                               | El logotipo como `<img>` si hay `LogoUrl`; si no, la marca en texto. |
| `Year`                                      | Año en curso, para el pie.                                         |
| `Preheader`                                 | El asunto ya sustituido.                                           |
| `Body`                                      | Solo en `Layout.html`: el interior del aviso.                      |

**En el `.html`, los valores se insertan codificados como HTML**; en el `.txt` y en el
asunto, crudos. `Body` y `BrandHeader` son las dos excepciones: son marcado que genera el
propio paquete, y codificarlos lo mostraría literal.

## Los enlaces

`ActionUrl` se construye con `ClientBaseUrl`, la ruta correspondiente y una cadena de
consulta con los datos que la pantalla tiene que devolver a la API. Los nombres de los
parámetros salen de los contratos de `Persiltech.Membership`, que identifican la cuenta por
el correo y no por el identificador:

| Aviso                  | Enlace                                                        |
| ---------------------- | ------------------------------------------------------------- |
| Confirmación de correo | `{ClientBaseUrl}{EmailConfirmationPath}?email=…&token=…`      |
| Reinicio de contraseña | `{ClientBaseUrl}{PasswordResetPath}?email=…&token=…`          |
| Cambio de correo       | `{ClientBaseUrl}{EmailChangePath}?newEmail=…&token=…`         |

Ambos valores viajan codificados con `Uri.EscapeDataString`: los testigos que genera
ASP.NET Core Identity traen `+` y `/`, y sin codificar se pierden por el camino.

# Tipos internos

No forman parte del contrato y pueden cambiar sin subir la versión mayor. Viven en
`Internal/`, y el proyecto de pruebas los ve por `InternalsVisibleTo`.

| Tipo                              | Papel                                                                                  |
| --------------------------------- | -------------------------------------------------------------------------------------- |
| `TemplatedMembershipEmailSender`  | Implementación de `IMembershipEmailSender`: arma los valores, construye el enlace, compone y entrega. |
| `EmbeddedTemplateRenderer`        | Implementación de `IEmailTemplateRenderer` sobre los recursos embebidos y el directorio de sustitución. |
| `MembershipEmailOptionsValidator` | `IValidateOptions<MembershipEmailOptions>` que comprueba las reglas al arrancar y acumula los fallos. |

# Decisiones de diseño

- **Plantillas en archivos, no en cadenas de C#.** El HTML de correo es tabular, con estilos
  en línea y parches para clientes antiguos: son cientos de líneas por aviso. En archivos se
  previsualizan en un navegador, las puede tocar un diseñador y el encabezado y el pie se
  escriben una sola vez.
- **Ni Razor ni un motor de plantillas de terceros en la primera versión.** Razor exige
  ASP.NET Core y compilación en tiempo de ejecución para tres correos transaccionales. La
  sustitución de marcadores cubre lo que hay; `IEmailTemplateRenderer` es la costura por la
  que entra un motor con condicionales el día que haga falta.
- **La marca y las rutas son configuración, no código.** Es lo que separa este paquete de un
  adaptador escrito dentro de una aplicación: dos consumidores con marcas distintas usan el
  mismo binario.
- **Un marcador desconocido lanza.** Es determinista —no depende de los datos del usuario—,
  así que salta en la primera prueba de quien escribe la plantilla.
- **El paquete no registra el transporte.** Elegir servidor SMTP y credenciales es del
  consumidor; aquí solo se consume `IEmailSender`.
- **El compositor es `Singleton` y cachea las plantillas.** Leer tres archivos por correo no
  aporta nada; a cambio, un cambio en el directorio de sustitución exige reiniciar.
- **Las opciones se validan con `IValidateOptions<MembershipEmailOptions>`**, no con
  anotaciones de datos. Es el idioma del resto de paquetes de Persiltech, y aquí paga solo:
  que `TemplatesDirectory` exista, que `PrimaryColor` sea un color hexadecimal y que las URL
  sean absolutas son reglas que ninguna anotación expresa, y las tres convierten fallos
  silenciosos en fallos de arranque.
- **El validador acumula los fallos** y se registra con `TryAddSingleton`, así que uno propio
  registrado antes gana.

# Fuera de alcance

- El puerto de SMS (`IMembershipSmsSender`): este paquete es solo de correo.
- Adjuntos, imágenes en línea e invitaciones de calendario.
- Localización de las plantillas por cultura. La resolución por nombre de archivo deja sitio
  para ello (`EmailConfirmation.es.html`), pero no se implementa aquí.
- Reintentos y colas: los hereda de `IEmailSender`, que envía en línea.
- Elegir el transporte. El paquete no depende de SMTP, sino de `IEmailSender`.

# Organización de los artefactos

| Archivo                                    | Ubicación                                    |
| ------------------------------------------ | -------------------------------------------- |
| `MembershipEmailOptions.cs`                | `src/Persiltech.Membership.Email/`           |
| `IEmailTemplateRenderer.cs`                | `src/Persiltech.Membership.Email/`           |
| `RenderedEmail.cs`                         | `src/Persiltech.Membership.Email/`           |
| `DependencyInjection.cs`                   | `src/Persiltech.Membership.Email/`           |
| `TemplatedMembershipEmailSender.cs`        | `src/Persiltech.Membership.Email/Internal/`  |
| `EmbeddedTemplateRenderer.cs`              | `src/Persiltech.Membership.Email/Internal/`  |
| `MembershipEmailOptionsValidator.cs`       | `src/Persiltech.Membership.Email/Internal/`  |
| `Layout.html`                              | `src/Persiltech.Membership.Email/Templates/` |
| `EmailConfirmation.{subject.txt,html,txt}` | `src/Persiltech.Membership.Email/Templates/` |
| `PasswordReset.{subject.txt,html,txt}`     | `src/Persiltech.Membership.Email/Templates/` |
| `EmailChange.{subject.txt,html,txt}`       | `src/Persiltech.Membership.Email/Templates/` |
