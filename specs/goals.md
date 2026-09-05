# Paquetería por Edinson Aldaz

## Descripción

Este es un monorepo de creación de paquetes para nuget.org desarrollados con tecnología .NET que podrán ser reutilizados en diferentes proyectos empresariales.

## Objetivo

Pretendo con el tiempo poder monetizar con ellos. Serán repositorios públicos en Github. La documentación se encontrará en mi página de protafolio https://aldazsoft.github.io/.
Este portafolio cuenta con un apartado para la documentación de cada paquete. Puedes revisar el código fuente en E:\Repos\Github\aldazsoft\aldazsoft.github.io.
Cada paquete tendrá su propia especificación dentro de la carpeta /specs de la raíz de este workspace. Cada especificación deberá permitir recrear desde cero el paquete, en caso sea necesario.
Cada vez que se crea y publica un nuevo paquete, este deberá ser sincronizado en el portafolio con su documentación correspondiente. El portafolio ya cuenta con skills que permite auditar y sincronizar los paquetes.

## Skills globales

Se cuenta con 2 skills globales para la creación e implementación de paquetes.

### Skill de scaffolding de paquete (scaffold-nuget-package)

Este skill se utilizará para inicializar un paquete con la estructura de la casa.

### Skill de implementación de paquete (implement-nuget-package)

Este skill se utilizará para implementar los artefactos de código de un paquete dentro de una solución existente.
