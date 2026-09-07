---
packageName: Persiltech.Membership.Blazor
title: Persiltech.Membership.Blazor
version: 0.1.0
tags: dotnet;csharp;blazor;webassembly;mudblazor;membership;identity;jwt;authentication
withTests: true
# El proyecto de verificación ya existe: samples/Persiltech.Membership.Blazor.Sample, que
# hoy consume la API de Persiltech.Membership redeclarando sus contratos. Al implementar
# este paquete pasa a consumirlo a él, y es lo que lo pone a prueba. Crear otro sample
# dejaría dos aplicaciones haciendo lo mismo.
withSample: false
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
# Vacío: lo declara Directory.Build.props para todo el monorepo, que es público.
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Membership.Blazor/
privateSource: false
noCi: false
intoMonorepo: true
---

# Propósito

Cliente Blazor de `Persiltech.Membership`: el estado de autenticación a partir del JWT que
emite la API, la renovación automática de la sesión con su testigo, el manejador que firma
cada petición, y los componentes de MudBlazor de las pantallas de sesión, contraseña,
correo, teléfono, perfil, doble factor, roles y usuarios.
