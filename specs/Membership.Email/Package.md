---
packageName: Persiltech.Membership.Email
title: Persiltech.Membership.Email
version: 0.1.0
tags: dotnet;csharp;email;membership;templates;smtp
withTests: true
withSample: true
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
# Vacío: lo declara Directory.Build.props para todo el monorepo, que es público.
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Membership.Email/
privateSource: false
noCi: false
---

# Propósito

Cerrar el puerto de salida de correo de `Persiltech.Membership`: compone los avisos de
confirmación del correo, reinicio de contraseña y cambio de correo a partir de plantillas
HTML, y los entrega por `Persiltech.Email`.

# Superficie pública

Ver `PublicApi.md`.

# Dependencias

Las dos son paquetes de la casa y van por `<ProjectReference>`, no por `<PackageReference>`:
dentro del monorepo el proyecto vecino es la fuente de verdad, y es `dotnet pack` quien lo
traduce a dependencia NuGet con la versión que ese proyecto tenga al empaquetar.

| Paquete | Por qué |
| --- | --- |
| `Persiltech.Membership` | Define `IMembershipEmailSender`, el puerto que este paquete implementa. |
| `Persiltech.Email` | El transporte. Este paquete redacta; no sabe de SMTP. |

Eso fija el orden de publicación: los dos base antes que este. Lo comprueba
`eng/Test-PublishReadiness.ps1` y lo refleja `specs/PublishOrder.md`.

# Decisiones de diseño

- **Las plantillas viajan embebidas en el ensamblado**, no como archivos sueltos del
  `.nupkg`. Así el paquete funciona sin que el consumidor copie nada, y quien quiera las
  suyas las sustituye por archivo con `MembershipEmailOptions.TemplatesDirectory`.
- **La marca es configuración**, no código: nombre, colores, logotipo y las rutas de la
  aplicación cliente salen de `MembershipEmailOptions`, validadas al arrancar.
- **Los valores se codifican en HTML** al componer el cuerpo. Un nombre con `<` no puede
  romper la maquetación ni inyectar marcado en el correo del destinatario.
- **El cuerpo en texto plano no se codifica**, porque ahí no hay marcado que escapar y
  hacerlo mostraría entidades al lector.

---

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera el flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó
> a posteriori a partir del `.csproj`, que es la fuente de verdad de la metadata.
