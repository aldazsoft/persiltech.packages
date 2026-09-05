# Persiltech.Membership.Email

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Membership.Email.svg)](https://www.nuget.org/packages/Persiltech.Membership.Email/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Adaptador de correo de [Persiltech.Membership](https://www.nuget.org/packages/Persiltech.Membership/):
compone los avisos de confirmación de correo, reinicio de contraseña y cambio de correo a
partir de plantillas HTML, y los entrega por
[Persiltech.Email](https://www.nuget.org/packages/Persiltech.Email/).

`Persiltech.Membership` deja `IMembershipEmailSender` sin implementar a propósito, porque
redactar el mensaje exige conocer la marca, el diseño y las rutas de la aplicación. Este
paquete pone esas tres cosas en `Options` y en plantillas sustituibles, de modo que dos
aplicaciones distintas comparten el mismo binario.

## Instalación

    dotnet add package Persiltech.Membership.Email

## Uso

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmtpEmailSender(options => builder.Configuration.GetSection("Smtp").Bind(options));
builder.Services.AddMembershipEmail(options => builder.Configuration.GetSection("MembershipEmail").Bind(options));
```

`AddMembershipEmail` **no registra el transporte**: `IEmailSender` lo aporta el consumidor,
normalmente con `AddSmtpEmailSender` de `Persiltech.Email`, porque elegir servidor y
credenciales es suyo. El orden entre ambas llamadas da igual; lo que no puede es faltar una.

Con la configuración correspondiente:

```json
{
  "MembershipEmail": {
    "BrandName": "Persiltech",
    "ClientBaseUrl": "https://app.example.com",
    "LogoUrl": "https://cdn.example.com/logo.png",
    "PrimaryColor": "#0d6efd",
    "SupportEmail": "soporte@example.com"
  }
}
```

Las opciones se validan **al arrancar la aplicación**, no en el primer aviso: el registro
usa un `IValidateOptions<MembershipEmailOptions>` y encadena `ValidateOnStart()`. Los fallos
salen todos juntos, no de uno en uno.

Dos de las reglas evitan fallos silenciosos que de otro modo nadie ve: `TemplatesDirectory`
tiene que existir —apuntando a un directorio que no está, se servirían las plantillas
embebidas y el rebrandeo no se aplicaría sin avisar— y `LogoUrl` tiene que ser absoluta,
porque los clientes de correo no resuelven rutas relativas. El validador se registra con
`TryAddSingleton`: uno propio registrado antes gana.

## El contrato

```csharp
public sealed class MembershipEmailOptions
{
    public string BrandName { get; set; } = string.Empty;      // obligatorio
    public string ClientBaseUrl { get; set; } = string.Empty;  // obligatoria, http o https absoluta

    public string EmailConfirmationPath { get; set; } = "/confirm-email";
    public string PasswordResetPath { get; set; } = "/reset-password";
    public string EmailChangePath { get; set; } = "/confirm-email-change";

    public string? LogoUrl { get; set; }                       // si se indica, absoluta
    public string PrimaryColor { get; set; } = "#0d6efd";      // #rgb o #rrggbb
    public string? SupportEmail { get; set; }

    public string? TemplatesDirectory { get; set; }            // si se indica, tiene que existir
}
```

`ClientBaseUrl` es la raíz de la aplicación **cliente** —la que abre el usuario—, no la de la
API, y de ella cuelgan las tres rutas.

## Los enlaces

Cada aviso lleva un enlace de vuelta con los datos que la pantalla tiene que devolver a la
API. Los nombres de los parámetros salen de los contratos de `Persiltech.Membership`, que
identifican la cuenta por el correo:

| Aviso                  | Enlace                                                   |
| ---------------------- | -------------------------------------------------------- |
| Confirmación de correo | `{ClientBaseUrl}{EmailConfirmationPath}?email=…&token=…` |
| Reinicio de contraseña | `{ClientBaseUrl}{PasswordResetPath}?email=…&token=…`     |
| Cambio de correo       | `{ClientBaseUrl}{EmailChangePath}?newEmail=…&token=…`    |

Ambos valores viajan codificados con `Uri.EscapeDataString`: los testigos de ASP.NET Core
Identity traen `+` y `/`, y sin codificar se pierden por el camino.

## Las plantillas

Cada aviso son tres archivos embebidos en el ensamblado, bajo `Templates/`:

| Aviso                  | Nombre              | Archivos                                            |
| ---------------------- | ------------------- | --------------------------------------------------- |
| Confirmación de correo | `EmailConfirmation` | `.subject.txt`, `.html`, `.txt`                     |
| Reinicio de contraseña | `PasswordReset`     | `.subject.txt`, `.html`, `.txt`                     |
| Cambio de correo       | `EmailChange`       | `.subject.txt`, `.html`, `.txt`                     |

El `.html` de cada aviso es solo el interior: el encabezado, el ancho de 600 píxeles y el pie
viven una vez en `Layout.html`, que envuelve a los tres. El `.txt` es la parte alternativa en
texto plano, que un correo transaccional no debería omitir.

### Cambiar el diseño

`TemplatesDirectory` apunta a una carpeta cuyos archivos ganan a los embebidos, por nombre.
Basta con dejar ahí el que se quiera cambiar —`Layout.html` para rebrandear entero, o el
`.html` de un aviso suelto—; el resto siguen saliendo del paquete. Las plantillas se leen una
vez y se cachean, así que un cambio en disco exige reiniciar la aplicación.

### Marcadores

La sintaxis es `{{Nombre}}`. Un marcador que no corresponda a ningún valor lanza
`InvalidOperationException` nombrándolo: una plantilla con un nombre mal tecleado tiene que
fallar en el primer envío de prueba, no llegar en blanco al buzón de un cliente.

| Marcador                                    | Origen                                                               |
| ------------------------------------------- | -------------------------------------------------------------------- |
| `FirstName`, `LastName`, `FullName`         | El aviso.                                                            |
| `Email`                                     | Destinatario del aviso.                                              |
| `ActionUrl`                                 | Enlace de vuelta, ya construido.                                     |
| `BrandName`, `PrimaryColor`, `SupportEmail` | Las opciones.                                                        |
| `BrandHeader`                               | El logotipo como `<img>` si hay `LogoUrl`; si no, la marca en texto. |
| `Year`                                      | Año en curso, para el pie.                                           |
| `Preheader`                                 | El asunto ya sustituido.                                             |
| `Body`                                      | Solo en `Layout.html`: el interior del aviso.                        |

En el `.html` los valores se insertan codificados —solo lo sensible en HTML: los acentos
viajan tal cual—; en el `.txt` y en el asunto, crudos. `Body` y `BrandHeader` son las dos
excepciones: son marcado que genera el propio paquete.

### Otro motor de plantillas

`IEmailTemplateRenderer` es público y se registra con `TryAddSingleton`, así que una
implementación propia —con Scriban, Fluid o lo que sea— registrada antes de
`AddMembershipEmail` gana:

```csharp
public interface IEmailTemplateRenderer
{
    RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string?> values);
}

public sealed record RenderedEmail(string Subject, string HtmlBody, string TextBody);
```

## Decisiones de diseño

- **Plantillas en archivos, no en cadenas de C#.** El HTML de correo es tabular y con estilos
  en línea: en archivos se previsualiza en un navegador, lo puede tocar un diseñador y el
  encabezado y el pie se escriben una sola vez.
- **Ni Razor ni un motor de terceros de partida.** Razor exige ASP.NET Core y compilación en
  tiempo de ejecución para tres correos transaccionales. `IEmailTemplateRenderer` es la
  costura por la que entra un motor con condicionales el día que haga falta.
- **La marca y las rutas son configuración.** Es lo que separa a este paquete de un adaptador
  escrito dentro de una aplicación.
- **Un marcador desconocido lanza.** Es determinista, así que salta en la primera prueba de
  quien escribe la plantilla.
- **El paquete no elige el transporte.** Depende de `IEmailSender`, no de SMTP.

## Compatibilidad

`net10.0`. Requiere `Persiltech.Membership` y `Persiltech.Email`.

## Estado

Versión `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

El código fuente no es público, así que esta tabla es el registro de cambios del paquete.

| Versión | Cambios                                                                                     |
| ------- | ------------------------------------------------------------------------------------------- |
| 0.1.0   | Primera versión: implementa `IMembershipEmailSender` con plantillas HTML embebidas y sustituibles, la marca y las rutas del cliente como configuración, y la entrega por `IEmailSender`. |

## Soporte

El código fuente de este paquete no es público. Para dudas, fallos o peticiones, usa la
[página del paquete](https://aldazsoft.github.io/Membership.Email/).

## Apoyar el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
