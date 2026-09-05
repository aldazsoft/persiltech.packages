---
# Único obligatorio.
packageName: Persiltech.UserServices

# Nombre legible que nuget.org usa como encabezado. Vacío = el packageName.
title:
# MAJOR.MINOR.PATCH. Versión inicial con la que se crea el paquete: se lee una
# sola vez, al generar la solución. A partir de ahí la versión vive en
# <VersionPrefix> del .csproj y editarla aquí no cambia nada.
version: 0.1.0
# PackageTags, separados por ";".
tags: dotnet;csharp;aspnetcore;authentication;identity;httpcontext;clean-architecture
# true genera tests/<packageName>.Tests (xUnit + NSubstitute) con
# InternalsVisibleTo. Déjalo en false para paquetes de puros contratos.
withTests: true

# --- Opcionales: vacío = valor por defecto -------------------------------
# Expresión SPDX. El archivo LICENSE solo se genera automáticamente para MIT.
license: MIT
# Ruta a un .png que se copia a assets/icon.png y se publica como PackageIcon.
iconPath: assets/icon.png
targetFramework: net10.0
# Se deducen de git y del nombre si se dejan vacíos:
#   author        -> git config user.name
#   company       -> primer segmento del packageName (Persiltech.XXX -> Persiltech)
#   repositoryUrl -> remoto "origin"
author:
company:
repositoryUrl:
# "Project website" en nuget.org. Vacío = el remoto sin el ".git" final, que solo
# sirve si el repositorio es público y hace de página del proyecto.
projectUrl: https://aldazsoft.github.io/UserServices/
# true cuando el código fuente no es público: omite la metadata de repositorio del
# .nuspec, que sería un enlace muerto, y apaga SourceLink.
privateSource: true
# true omite los workflows de GitHub Actions.
noCi: false
---

# Propósito

Implementación para ASP.NET Core de `IUserService` (el Output Port que define
`Persiltech.UserServices.Abstractions`), que resuelve la identidad y el estado de
autenticación del usuario actual a partir de `HttpContext.User`.
