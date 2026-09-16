---
packageName: Persiltech.Validation.Blazor
title: Persiltech.Validation.Blazor
version: 0.1.0
tags: dotnet;csharp;blazor;webassembly;mudblazor;validation;editcontext;problemdetails
withTests: true
withSample: true
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
repositoryUrl:
projectUrl: https://aldazsoft.github.io/Validation.Blazor/
privateSource: false
noCi: false
intoMonorepo: true
---

# Propósito

Llevar los errores de validación que devuelve una API al campo que los provocó: escribirlos en
el `EditContext` de Blazor para que el componente de entrada los muestre como si fueran suyos,
en lugar de amontonarlos en un cartel encima del formulario.

---

> **Nota sobre este archivo.** El paquete nació de extraer el `ModelValidator` que vivía en
> `Persiltech.ExceptionHandler.Blazor`, en el monorepo anterior, para que Megad y las
> aplicaciones que vengan lo compartan en vez de copiarlo. La fuente de verdad de la metadata
> —incluida la versión— es el `.csproj`.
>
> El proyecto de `samples/` es la aplicación **Blazor WebAssembly** de verificación, con una
> API de mentira que devuelve la forma exacta de `ValidationProblemDetails`. Tiene dos
> páginas: `/` es un registro corto, donde conviven las anotaciones de datos y los errores del
> servidor; `/controles` pone un control de cada clase —multilínea, desplegable único y
> múltiple, autocompletado, numérico, fecha, radio, casilla e interruptor— **sin ninguna
> anotación**, de modo que enviarlo vacío enseña de golpe cómo pinta cada uno el mensaje.
> **No son pruebas unitarias**: CI la compila, pero no la ejecuta. Existe porque lo que hay que
> comprobar —que el mensaje se pinte bajo el campo como si fuera nativo— no se ve en un aserto.
>
> Las pruebas están en `tests/Persiltech.Validation.Blazor.Tests` (xUnit, sin bUnit: montan el
> componente con un `Renderer` propio, porque el `EditContext` llega por cascada y un
> parámetro en cascada solo lo puede poner un renderizador).
