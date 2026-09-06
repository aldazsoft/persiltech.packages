# Orden de publicacion

<!-- Generado por eng/New-PublishOrder.ps1 a partir de los <ProjectReference> de src/.
     No lo edites a mano: vuelve a ejecutar el script cuando cambien las dependencias. -->

Publica de arriba abajo. Dentro de un mismo nivel el orden es indiferente y los
paquetes pueden publicarse en paralelo, porque no dependen entre si.

Esto importa porque las dependencias entre paquetes del monorepo van por
`<ProjectReference>`: es `dotnet pack` quien las traduce a dependencia NuGet tomando la
version que el proyecto vecino tenga al empaquetar. Publicar un consumidor antes que su
dependencia deja en nuget.org un paquete que apunta a una version que aun no existe, y
el consumidor falla al restaurar con NU1101 o NU1102.

Las dependencias externas no aparecen aqui: ya estan publicadas y no imponen orden.

## Nivel 1 - sin dependencias de la casa

| Paquete | Version | Depende de |
| --- | --- | --- |
| `Persiltech.Blazor.JSInterop` | 1.1.2 | - |
| `Persiltech.Email` | 0.1.1 | - |
| `Persiltech.Localizer` | 1.0.3 | - |
| `Persiltech.Membership` | 0.5.0 | - |
| `Persiltech.UserServices.Abstractions` | 0.1.15 | - |

## Nivel 2 - dependen del nivel 1

| Paquete | Version | Depende de |
| --- | --- | --- |
| `Persiltech.DomainValidation` | 2.0.3 | Persiltech.Localizer |
| `Persiltech.Membership.Email` | 0.1.0 | Persiltech.Email<br>Persiltech.Membership |
| `Persiltech.Membership.OAuth` | 0.2.0 | Persiltech.Membership |
| `Persiltech.Results` | 1.0.2 | Persiltech.Localizer |
| `Persiltech.UserServices` | 0.1.6 | Persiltech.UserServices.Abstractions |

---

Antes de publicar, `eng/Test-PublishReadiness.ps1` comprueba que el suelo declarado de
cada dependencia exista ya en nuget.org, que es la verificacion de que este orden se
respeto.
