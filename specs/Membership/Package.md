---
# Único obligatorio.
packageName: Persiltech.Membership

# Nombre legible que nuget.org usa como encabezado. Vacío = el packageName.
title:
# MAJOR.MINOR.PATCH. Versión inicial con la que se crea el paquete: se lee una
# sola vez, al generar la solución. A partir de ahí la versión vive en
# <VersionPrefix> del .csproj y editarla aquí no cambia nada.
version: 0.1.0
# PackageTags, separados por ";".
tags: dotnet;csharp;aspnetcore;identity;authentication;jwt;minimal-api;entity-framework-core
# true genera tests/<packageName>.Tests (xUnit + NSubstitute) con
# InternalsVisibleTo. Déjalo en false para paquetes de puros contratos.
withTests: true
# true genera samples/<packageName>.Sample: la app web que consume el paquete.
# Aquí es obligado: el paquete se compone en el arranque del consumidor
# (registro en DI, endpoints, DbContext) y eso no lo verifica una prueba unitaria.
withSample: true

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
# true omite los workflows de GitHub Actions.
noCi: false
---

# Propósito

Sistema de membresía reutilizable para aplicaciones ASP.NET Core: registro y
autenticación de usuarios sobre ASP.NET Core Identity, con endpoints de Minimal API
que el consumidor monta en la ruta que elija y emisión de un JSON Web Token firmado
con HMAC-SHA256.
