# Persiltech.Email

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Email.svg)](https://www.nuget.org/packages/Persiltech.Email/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Envío de correo electrónico por SMTP en .NET: el contrato `IEmailSender` y su implementación
con MailKit, con configuración por `Options` y registro en la inyección de dependencias.

El paquete **transporta, no redacta**. Recibe un asunto y un cuerpo ya compuestos y los
entrega al servidor. Quién elige plantilla, formato, idioma y enlaces es la aplicación
consumidora, que es la única que conoce sus propias rutas.

## Instalación

    dotnet add package Persiltech.Email

## El contrato

```csharp
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
}
```

`SendAsync` completa cuando el servidor SMTP **acepta** el mensaje. Eso no es la entrega al
buzón del destinatario: lo que ocurra después del salto está fuera del alcance de SMTP.

`To` admite la dirección sola (`juan@example.com`) o acompañada del nombre visible
(`Juan Pérez <juan@example.com>`), y se analiza al enviar. `TextBody` es el cuerpo
alternativo en texto plano: si es `null`, el mensaje viaja solo con la parte HTML.

El remitente no viaja en el mensaje, sino en las opciones, porque es propiedad de la cuenta
SMTP con la que se conecta y no de cada envío:

```csharp
public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;          // obligatorio
    public int Port { get; set; } = 587;                      // 1 – 65535
    public SmtpSecurity Security { get; set; } = SmtpSecurity.Auto;
    public string? UserName { get; set; }
    public string? Password { get; set; }                     // obligatoria si hay UserName
    public string FromAddress { get; set; } = string.Empty;   // obligatoria
    public string? FromDisplayName { get; set; }
    public int TimeoutInSeconds { get; set; } = 30;           // 1 – 600
}

public enum SmtpSecurity
{
    Auto = 0,
    None = 1,
    StartTls = 2,
    SslOnConnect = 3
}
```

`UserName` y `Password` son opcionales porque un relay local de desarrollo (Papercut,
MailHog) no autentica: si `UserName` viene vacío, no se llama a `Authenticate`.

`SmtpSecurity` decide el cifrado de la conexión: `Auto` lo deduce del puerto y de lo que
anuncie el servidor, `StartTls` es lo habitual en el 587, `SslOnConnect` en el 465, y `None`
solo tiene sentido contra un relay local.

## Uso

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmtpEmailSender(options => builder.Configuration.GetSection("Smtp").Bind(options));

var app = builder.Build();

app.MapPost("/email", async (
    SendEmailRequest request,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var message = new EmailMessage
    {
        To = request.To,
        Subject = request.Subject,
        HtmlBody = request.HtmlBody,
        TextBody = request.TextBody
    };

    await emailSender.SendAsync(message, cancellationToken);

    return Results.NoContent();
});

app.Run();
```

Con la configuración correspondiente:

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Security": "StartTls",
    "UserName": "no-reply@example.com",
    "Password": "...",
    "FromAddress": "no-reply@example.com",
    "FromDisplayName": "Persiltech"
  }
}
```

`AddSmtpEmailSender` valida las opciones **al arrancar la aplicación**, no en el primer
envío: registra un `IValidateOptions<SmtpOptions>` y encadena `ValidateOnStart()`, de modo
que un host vacío detiene el arranque en lugar de perder el primer correo que alguien
esperaba recibir. Los fallos salen **todos juntos**, no de uno en uno.

Las reglas son: `Host` y `FromAddress` obligatorios, `Port` entre 1 y 65535,
`TimeoutInSeconds` entre 1 y 600, y `Password` obligatoria cuando se configura `UserName`.
`FromAddress` se comprueba con el mismo analizador que la compondrá al enviar, así que lo
que pase aquí pasa también en tiempo de ejecución.

El validador se registra con `TryAddSingleton`: si prefieres tus propias reglas, registra un
`IValidateOptions<SmtpOptions>` antes de llamar a `AddSmtpEmailSender` y el tuyo gana.

De dónde salgan los valores es del consumidor: el delegado admite tanto el enlace de la
configuración como literales. La contraseña es un secreto y el paquete no la registra en
ningún log.

## Errores

Un destinatario que no se puede analizar sale como `ArgumentException` con
`ParamName = "To"`, **antes de conectar con nadie**: se comprueba con el mismo criterio con
el que se valida `FromAddress` al arrancar, así que una dirección sin dominio no llega a
salir por el cable para que la rechace el servidor en el `RCPT TO`.

Lo demás **no se envuelve**: un servidor inalcanzable o unas credenciales rechazadas salen
tal cual, como excepciones de MailKit o de sockets. Si tu aplicación quiere una categoría
propia de error, la envuelve ella, que es quien sabe qué hacer con cada caso.

Tampoco reintenta: el envío es un intento, síncrono respecto de quien llama.

## Decisiones de diseño

- **El paquete transporta, no redacta.** Recibe el asunto y el cuerpo ya compuestos.
  Redactar aquí obligaría a elegir plantilla, formato e idioma, y a inventarse las rutas de
  una aplicación que no es la suya.
- **`SmtpSecurity` es un tipo propio, y no el de MailKit.** Así el arranque del consumidor
  no necesita conocer la biblioteca de transporte, que es un detalle de implementación y no
  aparece en la superficie pública.
- **La implementación no se expone.** El consumidor resuelve `IEmailSender`; publicar el
  nombre de la clase convertiría en contrato algo que no lo es.
- **Una conexión por envío.** El cliente SMTP de MailKit no es seguro para uso concurrente,
  así que cada envío abre la suya, envía y la cierra. Un pool compartido es una optimización
  que este paquete no ha demostrado necesitar.
- **Sin dependencia de ASP.NET Core.** Sirve igual a una API web, a un worker o a una
  aplicación de consola.

Quedan fuera, de momento: varios destinatarios, copia y copia oculta, adjuntos, plantillas,
reintentos y colas, y cualquier transporte que no sea SMTP.

## Compatibilidad

`net10.0`.

## Estado

Versión `0.x`: la superficie pública puede cambiar entre versiones menores.

## Historial de versiones

El código fuente no es público, así que esta tabla es el registro de cambios del paquete.

| Versión | Cambios                                                                                     |
| ------- | ------------------------------------------------------------------------------------------- |
| 0.1.0   | Primera versión: el contrato `IEmailSender`, el `EmailMessage` y la implementación SMTP con MailKit, con las opciones validadas al arrancar. |

## Soporte

El código fuente de este paquete no es público. Para dudas, fallos o peticiones, usa la
[página del paquete](https://aldazsoft.github.io/Email/).

## Apoyar el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
