---
packageName: Persiltech.DomainValidation
title: "Clean Architecture: DomainValidation"
version: 2.0.1
tags: clean-architecture;business-rules;validation;dotnet;csharp;architecture
withTests: true
withSample: false
license: MIT
iconPath: assets/icon.png
targetFramework: net10.0
author: Edinson Aldaz
company: Persiltech
repositoryUrl:
projectUrl: https://aldazsoft.github.io/DomainValidation/
privateSource: false
noCi: false
---

# Propósito

Validar las reglas de negocio de una entidad dentro del dominio, con el patrón Specification:
cada regla es una clase que declara qué debe cumplir la entidad, y un validador las evalúa
todas y reúne los errores en un `ValidationResult` sin lanzar excepciones salvo que se le pida.

---

> **Nota sobre este archivo.** El paquete se escribió a mano, fuera del flujo de
> `scaffold-nuget-package`, así que esta especificación no fue su entrada: se reconstruyó a
> partir del `.csproj` al homologarlo, para que el repositorio tenga la misma forma que los
> que sí nacieron de ese flujo. Como en cualquier paquete, la fuente de verdad de la metadata
> —incluida la versión— es el `.csproj`: editar aquí el `title`, los `tags` o la `version` no
> cambia nada por sí solo.
