---
# Único obligatorio.
packageName: Persiltech.UserServices.Abstractions

# Nombre legible que nuget.org usa como encabezado. Vacío = el packageName.
title:
# MAJOR.MINOR.PATCH. Versión inicial con la que se crea el paquete: se lee una
# sola vez, al generar la solución. A partir de ahí la versión vive en
# <VersionPrefix> del .csproj y editarla aquí no cambia nada.
version: 0.1.4
# PackageTags, separados por ";".
tags: dotnet;csharp;authentication;identity;contracts;clean-architecture
# true genera tests/<packageName>.Tests (xUnit + NSubstitute) con
# InternalsVisibleTo. Déjalo en false para paquetes de puros contratos.
withTests: false
# true genera samples/<packageName>.Sample, una app web que consume el paquete para
# verificarlo en un arranque real. Innecesario en paquetes de puros contratos.
withSample: false

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
# "Project website" de la ficha de nuget.org. NO se deja vacío: el valor deducido es
# el remoto de git, y la documentación de cada paquete vive en el portafolio, en
# {siteUrl}/{route}/ (ver specs/Packages.md de aldazsoft.github.io). La metadata de
# nuget.org es inmutable por versión, así que un valor equivocado no se corrige.
projectUrl: https://aldazsoft.github.io/UserServices.Abstractions/
# true si el código fuente no es público. false de forma deliberada: la monetización
# va por los términos de la licencia, no por ocultar el código, y el código público
# es lo que da adopción, confianza y SourceLink.
privateSource: false
# true omite los workflows de GitHub Actions.
noCi: false
---

# Propósito

Define la interfaz `IUserService`, un Output Port que expone el estado de autenticación y la identidad del usuario actual, para que cualquier solución basada en Arquitectura Limpia lo consuma.
