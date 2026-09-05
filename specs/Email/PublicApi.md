---
# Paquete al que pertenece esta superficie pública. Determina el .slnx de la raíz
# y el proyecto src/<packageName>/ sobre los que se escribe el código.
packageName: Persiltech.Email

# MAJOR.MINOR.PATCH de la próxima publicación. Es el campo que se sube para
# preparar una nueva versión: se propaga a <VersionPrefix> del .csproj, que es
# la versión que acaba en nuget.org.
version: 0.1.0
---

# Superficie pública

El paquete entrega por SMTP un mensaje **ya redactado**. Quién lo redacta —asunto, cuerpo y
enlaces— es del consumidor, y queda _Fuera de alcance_.

Todos los tipos públicos viven en la raíz del proyecto, en el namespace `Persiltech.Email`:
son exactamente los que el consumidor nombra al componer su aplicación.

## IEmailSender

Puerto de envío. Es el tipo que el consumidor resuelve del contenedor; la implementación no
se expone.

| Miembro                                                                     | Descripción                                                              |
| --------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `Task SendAsync(EmailMessage message, CancellationToken cancellationToken)` | Entrega el mensaje. La tarea completa cuando el servidor SMTP lo acepta. |

El contrato no promete entrega al buzón del destinatario, solo aceptación por parte del
servidor: lo que ocurra después está fuera del alcance de SMTP.

## EmailMessage

Mensaje a enviar. `sealed record` con propiedades `init`.

```csharp
public sealed record EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
}
```

| Miembro                           | Descripción                                                                                                             |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `string To { get; init; }`        | Destinatario. Admite la dirección sola (`juan@example.com`) o con nombre (`Juan Pérez <juan@example.com>`). Obligatorio. Se analiza al enviar, no al construir el mensaje. |
| `string Subject { get; init; }`   | Asunto. Obligatorio.                                                                                                     |
| `string HtmlBody { get; init; }`  | Cuerpo en HTML. Obligatorio.                                                                                             |
| `string? TextBody { get; init; }` | Cuerpo alternativo en texto plano. Opcional: si es `null`, el mensaje viaja solo con la parte HTML.                      |

`required` obliga a poblar los tres primeros en el inicializador de objeto, así que un
mensaje a medio construir no compila.

## SmtpOptions

Opciones de conexión con el servidor. Clase `sealed`, que el consumidor rellena con el
delegado `Action<SmtpOptions>` de `AddSmtpEmailSender`.

| Miembro                                 | Descripción                                                         |
| --------------------------------------- | ------------------------------------------------------------------- |
| `string Host { get; set; }`             | Nombre o dirección del servidor SMTP. Obligatorio.                  |
| `int Port { get; set; }`                | Puerto del servidor. Por defecto `587`. Entre 1 y 65535.            |
| `SmtpSecurity Security { get; set; }`   | Cómo se cifra la conexión. Por defecto `SmtpSecurity.Auto`.         |
| `string? UserName { get; set; }`        | Usuario de autenticación. Opcional.                                 |
| `string? Password { get; set; }`        | Contraseña de autenticación. Obligatoria si hay `UserName`.         |
| `string FromAddress { get; set; }`      | Dirección del remitente de todos los mensajes. Obligatoria.         |
| `string? FromDisplayName { get; set; }` | Nombre visible del remitente. Opcional.                             |
| `int TimeoutInSeconds { get; set; }`    | Espera máxima de las operaciones con el servidor. Por defecto `30`. Entre 1 y 600. |

Las reglas se comprueban **al arrancar la aplicación**, no en el primer envío:
`AddSmtpEmailSender` registra un `IValidateOptions<SmtpOptions>` y encadena
`ValidateOnStart()`. El validador **acumula** todos los fallos en una sola respuesta.

`FromAddress` se valida con el mismo analizador que la compondrá al enviar
(`MailboxAddress.TryParse` de MimeKit), con `AllowAddressesWithoutDomain` en `false`: por
defecto, ese analizador da por buena una dirección sin dominio que ningún servidor SMTP
aceptaría. **Ese criterio es único en el paquete**: el destinatario de cada mensaje se
analiza igual, para que una dirección que el arranque da por mala no sea aceptable en un
envío, ni al revés.

`UserName` y `Password` son opcionales porque un relay local de desarrollo (Papercut,
MailHog) no autentica. Si `UserName` viene vacío, no se llama a `Authenticate`. La
contraseña es un secreto: la aporta el consumidor desde su configuración, y el paquete no
la registra en ningún log.

El remitente vive en las opciones y no en el mensaje: es propiedad de la cuenta SMTP con la
que se conecta, no de cada envío.

## SmtpSecurity

Cómo se cifra la conexión. `enum`.

| Miembro        | Descripción                                                                  |
| -------------- | ---------------------------------------------------------------------------- |
| `Auto`         | Se decide por el puerto y por lo que anuncie el servidor. Valor por defecto. |
| `None`         | Sin cifrado. Solo para un relay local de desarrollo.                         |
| `StartTls`     | Conexión en claro que se eleva a TLS con `STARTTLS`. Lo habitual en el 587.  |
| `SslOnConnect` | TLS desde el primer byte. Lo habitual en el 465.                             |

## DependencyInjection

Registro en el contenedor. Clase `static` en la raíz del proyecto.

| Miembro                                                                                                               | Descripción                                                                           |
| --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `static IServiceCollection AddSmtpEmailSender(this IServiceCollection services, Action<SmtpOptions> configureOptions)` | Registra `IEmailSender` con la implementación SMTP y valida las opciones al arrancar. |

Devuelve la misma colección, para poder encadenar. Lanza `ArgumentNullException` si falta
cualquiera de los dos argumentos.

Deja registrados exactamente estos cuatro servicios:

| Servicio                         | Implementación         | Tiempo de vida | Cómo                             |
| -------------------------------- | ---------------------- | -------------- | -------------------------------- |
| `IValidateOptions<SmtpOptions>`  | `SmtpOptionsValidator` | `Singleton`    | `TryAddSingleton`                |
| `IOptions<SmtpOptions>`          | —                      | `Singleton`    | `AddOptions().Configure().ValidateOnStart()` |
| `ISmtpClientFactory`             | `SmtpClientFactory`    | `Singleton`    | `AddSingleton`                   |
| `IEmailSender`                   | `SmtpEmailSender`      | `Scoped`       | `AddScoped`                      |

La fábrica es `Singleton` porque no tiene estado: lo que no se puede compartir es el cliente
que fabrica, no ella. El remitente es `Scoped` por la regla de la casa, y puede serlo porque
no guarda nada entre llamadas.

# Comportamiento del envío

`SendAsync` hace siempre esta secuencia, y no reintenta ninguno de los pasos:

1. Rechaza con `ArgumentNullException` un `message` nulo.
2. Compone el `MimeMessage`. Va **antes** de conectar: un destinatario inválido no merece
   abrir un socket, y así el error llega de inmediato en lugar de tras el saludo del servidor.
3. Pide un cliente a `ISmtpClientFactory` y lo desecha al terminar, pase lo que pase.
4. Le fija el tiempo de espera: `TimeoutInSeconds` convertido a milisegundos.
5. Conecta con `Host`, `Port` y la traducción de `Security` a `SecureSocketOptions`.
6. Autentica **solo** si `UserName` trae algo que no sea espacio en blanco.
7. Envía el mensaje.
8. Cierra la sesión con `QUIT` (`DisconnectAsync(quit: true)`), no dejando caer el socket.

El mensaje se compone así:

- Remitente: `FromAddress` con `FromDisplayName` como nombre visible; sin nombre visible, la
  dirección sola.
- Destinatario: `To`, analizado con **el mismo criterio** que valida `FromAddress` al
  arrancar. Si no se puede analizar, `SendAsync` lanza `ArgumentException` con
  `ParamName = "To"` y la dirección en el mensaje, sin haber conectado con nadie.
- Cuerpo: `HtmlBody` siempre; `TextBody` se añade como parte alternativa **si trae algo que
  no sea espacio en blanco**, y entonces el mensaje viaja como `multipart/alternative`.

**Los fallos del transporte se propagan tal cual**: el paquete no envuelve las excepciones
de MailKit ni las traduce a un tipo propio. Un servidor inalcanzable o unas credenciales
rechazadas salen de `SendAsync` como lo que son. La excepción es el destinatario inválido,
que es un error del argumento y no del transporte, y por eso sale como `ArgumentException`.

# Tipos internos

No forman parte del contrato y pueden cambiar sin subir la versión mayor. Viven en
`Internal/`, y el proyecto de pruebas los ve por `InternalsVisibleTo`.

| Tipo                   | Papel                                                                                            |
| ---------------------- | ------------------------------------------------------------------------------------------------ |
| `SmtpEmailSender`      | Implementación de `IEmailSender`: compone el `MimeMessage`, conecta, autentica, envía y cierra.   |
| `ISmtpClientFactory`   | Costura que aísla la creación del cliente de MailKit, para poder verificar el envío sin servidor. |
| `SmtpClientFactory`    | Implementación por defecto de la costura: devuelve un `SmtpClient` de MailKit.                    |
| `SmtpOptionsValidator` | `IValidateOptions<SmtpOptions>` que comprueba las reglas al arrancar y acumula los fallos.        |
| `EmailAddressParsing`  | Las `ParserOptions` con las que el paquete analiza toda dirección, remitente y destinatario.      |

# Decisiones de diseño

- **El paquete transporta, no redacta.** Recibe un asunto y un cuerpo ya compuestos. Quien
  elige plantilla, formato, idioma y enlaces es el consumidor, que es el único que conoce
  las rutas de su aplicación.
- **`SmtpSecurity` es propio, y no el `SecureSocketOptions` de MailKit.** Si las opciones
  expusieran el tipo de MailKit, el arranque del consumidor necesitaría un `using` de
  MailKit y el paquete dejaría de poder cambiar de biblioteca sin romper el contrato.
  MailKit es un detalle de implementación, y la superficie pública no lo menciona.
- **`SmtpEmailSender` es `internal`.** El consumidor resuelve `IEmailSender`; el nombre de
  la implementación no le hace falta, y publicarlo convertiría en contrato algo que no lo es.
- **Una conexión por envío.** El `SmtpClient` de MailKit no es seguro para uso concurrente,
  así que cada `SendAsync` crea el suyo, conecta, envía y lo cierra. Mantener una conexión
  abierta y compartida exigiría un pool con sincronización, y eso es una optimización que un
  paquete de 0.1.0 no ha demostrado necesitar.
- **Las opciones se validan en el arranque con `IValidateOptions<SmtpOptions>`**, no con
  anotaciones de datos. Es el idioma del resto de paquetes de Persiltech, y aquí hace falta
  igualmente: que `Password` sea obligatoria solo cuando hay `UserName` es una regla entre
  campos que una anotación no expresa, y validar `FromAddress` con el analizador de MimeKit
  —el mismo que la compondrá al enviar— dice la verdad, mientras que `[EmailAddress]` es más
  laxo que él. Un host vacío detiene la aplicación al arrancar, no en el primer correo que
  alguien esperaba recibir.
- **El validador acumula los fallos.** Un despliegue mal configurado los ve todos en el
  primer arranque, en vez de descubrirlos de uno en uno a base de reinicios.
- **El validador se registra con `TryAddSingleton`**, así que un `IValidateOptions<SmtpOptions>`
  propio registrado antes gana.
- **Un solo criterio para analizar direcciones**, compartido por el validador y la
  composición del mensaje. Con dos criterios, el remitente se comprobaría estricto al
  arrancar y el destinatario laxo al enviar: una dirección sin dominio pasaría el filtro,
  saldría por el cable y la rechazaría el servidor en el `RCPT TO`, que es el peor sitio
  donde enterarse.
- **Sin dependencia de ASP.NET Core.** No hay `FrameworkReference`: el paquete sirve igual a
  una API web, a un worker o a una aplicación de consola.
- **El registro no elige por el consumidor de dónde salen las opciones.** Se rellenan con un
  `Action<SmtpOptions>`; si vienen de `IConfiguration`, es el consumidor quien las enlaza.

# Fuera de alcance

- Varios destinatarios, copia, copia oculta, adjuntos y cabeceras propias.
- Plantillas, composición de cuerpos y localización de los textos.
- Reintentos, colas y envío en segundo plano. El envío es síncrono respecto de quien llama.
- Otros transportes (SendGrid, Microsoft Graph, Amazon SES) y el envío por SMS.
- Firma DKIM y reputación del remitente.

# Organización de los artefactos

| Archivo                  | Ubicación                        |
| ------------------------ | -------------------------------- |
| `IEmailSender.cs`        | `src/Persiltech.Email/`          |
| `EmailMessage.cs`        | `src/Persiltech.Email/`          |
| `SmtpOptions.cs`         | `src/Persiltech.Email/`          |
| `SmtpSecurity.cs`        | `src/Persiltech.Email/`          |
| `DependencyInjection.cs` | `src/Persiltech.Email/`          |
| `ISmtpClientFactory.cs`  | `src/Persiltech.Email/Internal/` |
| `SmtpClientFactory.cs`   | `src/Persiltech.Email/Internal/` |
| `SmtpEmailSender.cs`      | `src/Persiltech.Email/Internal/` |
| `SmtpOptionsValidator.cs` | `src/Persiltech.Email/Internal/` |
| `EmailAddressParsing.cs`  | `src/Persiltech.Email/Internal/` |

El proyecto declara además `GlobalUsings.cs` en su raíz, por la convención de la casa: los
archivos no llevan directivas `using` propias.
