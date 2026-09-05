---
packageName: Persiltech.Blazor.JSInterop
title: Persiltech.Blazor.JSInterop
version: 1.1.0
tags: dotnet;csharp;blazor;jsinterop;webassembly;wasm
withTests: true
withSample: true
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Blazor.JSInterop/
privateSource: true
noCi: false
---

# Propósito

Dar la base de la que heredan los servicios de Blazor que llaman a un módulo de JavaScript o a
un módulo de WebAssembly: importarlo una sola vez y de forma perezosa, invocar sus funciones y
liberarlo con el componente.

---

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera el flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó al
> homologarlo. La fuente de verdad de la metadata —incluida la versión— es el `.csproj`.
>
> El proyecto de `samples/` es la aplicación Blazor WebAssembly de verificación.
> **No son pruebas unitarias**: CI la compila, pero no la ejecuta.
> Consume el cargador `_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js` que publica el
> paquete, a propósito, para que un recurso estático roto no vuelva a pasar inadvertido.
>
> Las pruebas de verdad están en `tests/Persiltech.Blazor.JSInterop.Tests` (xUnit, con dobles
> de `IJSRuntime` e `IJSObjectReference` escritos a mano, sin bUnit): cubren la importación
> perezosa y su reintento tras un fallo, el paso de argumentos, la liberación de referencias y
> la tolerancia a un circuito caído.
