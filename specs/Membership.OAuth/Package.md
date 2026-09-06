---
packageName: Persiltech.Membership.OAuth
title: Persiltech.Membership.OAuth
version: 0.1.0
tags: dotnet;csharp;aspnetcore;oauth2;openid-connect;openiddict;identity;pkce;minimal-api
# El paquete no tiene proyecto de pruebas propio: sus pruebas viven en
# tests/Persiltech.Membership.Tests, que ya levanta la aplicación entera con el
# paquete base. Separarlas obligaría a duplicar ese arranque.
withTests: false
# El sample también es compartido: samples/Persiltech.Membership.Sample monta los
# dos paquetes, que es como se consumen.
withSample: false
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
# Vacío: lo declara Directory.Build.props para todo el monorepo, que es público.
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Membership.OAuth/
privateSource: false
noCi: false
---

# Propósito

Convertir a `Persiltech.Membership` en un servidor de autorización OAuth 2.0 y OpenID
Connect, sobre OpenIddict: flujo Authorization Code con PKCE, credenciales de cliente y
renovación por *refresh token*, emitiendo para las mismas cuentas de ASP.NET Core Identity
que administra el paquete base.

# Superficie pública

Ver `PublicApi.md`.

# Dependencias

| Referencia | Por qué |
| --- | --- |
| `Persiltech.Membership` (proyecto) | Las cuentas y el `ApplicationUser` para los que se emiten los testigos. |
| `OpenIddict.AspNetCore` | El servidor de autorización. No se reimplementa el protocolo. |
| `OpenIddict.EntityFrameworkCore` | El almacén de aplicaciones, autorizaciones y testigos. |
| `<FrameworkReference Microsoft.AspNetCore.App />` | Enrutamiento y autenticación, que vienen en el framework compartido. |

# Decisiones de diseño

- **El servidor no monta esquema interactivo ni pantalla de inicio de sesión.** El flujo de
  código exige una sesión de navegador, pero la interfaz es del consumidor: el paquete solo
  declara qué esquema usar, con `InteractiveAuthenticationScheme`.
- **El token de acceso no se cifra.** OpenIddict lo cifra por defecto, y eso obligaría a todo
  servidor de recursos a usar su validación propia. Sin cifrar es un JWT que cualquier
  middleware estándar valida, y eso lo mantiene intercambiable con los que emite el paquete
  base.
- **PKCE es obligatorio** en el flujo de código: `RequireProofKeyForCodeExchange()`. Una
  petición sin desafío se rechaza de plano con 400, sin llegar a redirigir.
- **Un cliente sin secreto es público** y solo puede usar Authorization Code con PKCE; uno con
  secreto es confidencial y puede además pedir credenciales de cliente. Lo decide
  `MembershipOAuthClient.ClientSecret`, y el alta es idempotente.
- **La revocación no lleva *passthrough*.** OpenIddict la resuelve por completo contra su
  propio almacén, y un manejador propio solo podría estorbar.
- **Contexto de datos aparte.** `MembershipOAuthDbContext` no comparte tablas con
  `MembershipDbContext`, así que sus migraciones viven en su propia carpeta y se generan con
  `--context`. Ver el `README.md` de la raíz.

# Aviso de uso

`UseDevelopmentCertificates` genera certificados de firma y cifrado efímeros: sirve para
desarrollar, y **no debe quedar activo en producción**, donde los certificados los aporta el
consumidor por el punto de extensión `configureServer`.

---

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera el flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó
> a posteriori a partir del `.csproj`, que es la fuente de verdad de la metadata.
