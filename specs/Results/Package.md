---
packageName: Persiltech.Results
title: Persiltech.Results
version: 1.0.1
tags: dotnet;csharp;result;railway;error-handling;functional
withTests: true
withSample: false
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Results/
privateSource: true
noCi: false
---

# Propósito

Implementar el patrón Result: que una operación devuelva su éxito o su fallo **como valor**,
con mensajes de error localizados, en lugar de lanzar excepciones para el flujo previsible.

---

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera el flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó al
> homologarlo. La fuente de verdad de la metadata —incluida la versión— es el `.csproj`.
>
> Es el **primer paquete de la flota con pruebas unitarias de verdad** en `tests/`: xUnit v3,
> con 7 pruebas que CI ejecuta. En los anteriores, lo que las acompañaba eran aplicaciones de
> verificación, que van a `samples/`.
>
> **Sustituye al legacy `Persiltech.Result`** (singular, `1.0.6`). El CPM ya ofrece el plural,
> para que ningún proyecto nuevo caiga en el viejo.
