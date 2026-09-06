---
packageName: Persiltech.Email
title: Persiltech.Email
# Versión con la que se creó el paquete. A partir de ahí la fuente de verdad es
# <VersionPrefix> del .csproj; editarla aquí no cambia nada.
version: 0.1.0
tags: dotnet;csharp;email;smtp;mailkit
withTests: true
withSample: true
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
# Vacío: lo declara Directory.Build.props para todo el monorepo, que es público.
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Email/
privateSource: false
noCi: false
---

# Propósito

Enviar correo por SMTP: el contrato `IEmailSender` y su implementación con MailKit, con la
configuración por `Options` y su registro en la inyección de dependencias.

# Superficie pública

Ver `PublicApi.md`, que es la especificación de lo que el paquete expone.

# Dependencias

| Paquete | Por qué |
| --- | --- |
| `MailKit` | El cliente SMTP. `SmtpClient` de `System.Net.Mail` está obsoleto y no cubre STARTTLS ni la autenticación moderna. |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | El método de extensión de registro. Solo las abstracciones: el contenedor lo elige el consumidor. |
| `Microsoft.Extensions.Hosting.Abstractions` | La validación de opciones al arrancar, que falla el proceso en vez de en el primer envío. |

No declara `<FrameworkReference Include="Microsoft.AspNetCore.App" />`: el paquete no necesita
nada de ASP.NET Core y añadirlo lo ataría a aplicaciones web.

# Decisiones de diseño

- **El contrato y la implementación viajan juntos.** A diferencia de `UserServices`, aquí no
  hay un paquete de puros contratos aparte: `IEmailSender` no se implementa de varias formas
  en la misma solución, así que separarlo solo añadiría un paquete que mantener.
- **Las opciones se validan al arrancar**, con `ValidateDataAnnotations().ValidateOnStart()`.
  Un servidor SMTP mal configurado se descubre al desplegar, no cuando alguien pide un
  correo.
- **Remitente y destinatario se analizan con el mismo criterio**, que rechaza direcciones sin
  dominio.

---

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera el flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó
> a posteriori a partir del `.csproj`, que es la fuente de verdad de la metadata.
