#Requires -Version 7.0

<#
.SYNOPSIS
    Genera el orden de publicacion de los paquetes a partir de sus <ProjectReference>.

.DESCRIPTION
    El orden no se escribe a mano: se deduce del grafo real de dependencias entre los
    proyectos de src/. Cada paquete cae en el nivel siguiente al mas alto de sus
    dependencias de la casa, de modo que publicar de arriba abajo garantiza que ninguna
    dependencia llegue a nuget.org despues del paquete que la declara.

    Solo cuentan las dependencias entre paquetes del monorepo. Un <PackageReference> a un
    paquete de terceros no impone orden alguno: ya esta publicado. Por eso este archivo
    no reproduce la distincion entre "sin dependencias" y "solo con dependencias
    externas" que traia el orden del monorepo antiguo; para publicar, ambos casos son el
    mismo nivel.

.PARAMETER Path
    Archivo a escribir. Por defecto specs/PublishOrder.md en la raiz del repositorio.

.PARAMETER Check
    No escribe nada: compara lo generado con lo que hay en disco y termina con codigo 1
    si difieren. Pensado para CI, que asi detecta un archivo desactualizado.

.EXAMPLE
    ./eng/New-PublishOrder.ps1

.EXAMPLE
    ./eng/New-PublishOrder.ps1 -Check
#>

[CmdletBinding()]
param(
    [string] $Path,
    [switch] $Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $Path) { $Path = Join-Path $repositoryRoot 'specs/PublishOrder.md' }

function Get-ProjectText {
    param([string] $ProjectPath)
    [xml] (Get-Content -LiteralPath $ProjectPath -Raw)
}

# Solo los proyectos empaquetables: un <PackageId> es lo que distingue a un paquete de un
# proyecto de apoyo.
$package = @{}
foreach ($file in Get-ChildItem -Path (Join-Path $repositoryRoot 'src') -Filter '*.csproj' -Recurse -File) {
    $document = Get-ProjectText -ProjectPath $file.FullName

    $identifier = $document.SelectSingleNode("//*[local-name()='PackageId']")
    if (-not $identifier) { continue }

    $version = $document.SelectSingleNode("//*[local-name()='VersionPrefix']")

    $reference = foreach ($node in $document.SelectNodes("//*[local-name()='ProjectReference']")) {
        $include = $node.GetAttribute('Include')
        if (-not $include) { continue }
        $resolved = Join-Path $file.DirectoryName ($include -replace '\\', [System.IO.Path]::DirectorySeparatorChar)
        (Resolve-Path -LiteralPath $resolved).Path
    }

    $package[$file.FullName] = [pscustomobject]@{
        Id           = $identifier.InnerText
        Version      = if ($version) { $version.InnerText } else { '(sin VersionPrefix)' }
        ProjectPath  = $file.FullName
        References   = @($reference)
        Dependencies = @()
        Level        = -1
    }
}

if ($package.Count -eq 0) { throw "No se encontro ningun proyecto con <PackageId> bajo src/." }

# Las rutas referenciadas se traducen a identificadores de paquete. Una referencia a un
# proyecto no empaquetable se ignora: no impone orden de publicacion.
foreach ($entry in $package.Values) {
    # El @() envuelve el resultado ordenado, no solo el foreach: sin el, un proyecto sin
    # dependencias asignaria $null en vez de un arreglo vacio, y al recorrerlo despues el
    # pipeline dejaria pasar un elemento nulo.
    # Ojo con el nombre de esta variable: PowerShell no distingue mayusculas, asi que un
    # $path aqui pisaria el parametro $Path y el archivo se escribiria sobre un .csproj.
    $resolved = foreach ($referencePath in $entry.References) {
        if ($package.ContainsKey($referencePath)) { $package[$referencePath].Id }
    }
    $entry.Dependencies = @($resolved | Sort-Object)
}

$byId = @{}
foreach ($entry in $package.Values) { $byId[$entry.Id] = $entry }

# Nivel = 1 + el mayor nivel de sus dependencias. Se resuelve por pasadas sucesivas, que
# ademas delatan un ciclo: si una pasada no asigna nada y quedan pendientes, hay ciclo.
$pending = [System.Collections.Generic.List[object]]::new()
$package.Values | ForEach-Object { $pending.Add($_) }

while ($pending.Count -gt 0) {
    $ready = @($pending | Where-Object {
        $unresolved = @($_.Dependencies | Where-Object { $byId[$_].Level -lt 0 })
        $unresolved.Count -eq 0
    })

    if ($ready.Count -eq 0) {
        $names = ($pending | ForEach-Object { $_.Id }) -join ', '
        throw "Ciclo de dependencias entre paquetes: $names"
    }

    foreach ($entry in $ready) {
        $entry.Level = if ($entry.Dependencies.Count -eq 0) {
            1
        }
        else {
            1 + (($entry.Dependencies | ForEach-Object { $byId[$_].Level }) | Measure-Object -Maximum).Maximum
        }
        [void] $pending.Remove($entry)
    }
}

$line = [System.Collections.Generic.List[string]]::new()
$line.Add('# Orden de publicacion')
$line.Add('')
$line.Add('<!-- Generado por eng/New-PublishOrder.ps1 a partir de los <ProjectReference> de src/.')
$line.Add('     No lo edites a mano: vuelve a ejecutar el script cuando cambien las dependencias. -->')
$line.Add('')
$line.Add('Publica de arriba abajo. Dentro de un mismo nivel el orden es indiferente y los')
$line.Add('paquetes pueden publicarse en paralelo, porque no dependen entre si.')
$line.Add('')
$line.Add('Esto importa porque las dependencias entre paquetes del monorepo van por')
$line.Add('`<ProjectReference>`: es `dotnet pack` quien las traduce a dependencia NuGet tomando la')
$line.Add('version que el proyecto vecino tenga al empaquetar. Publicar un consumidor antes que su')
$line.Add('dependencia deja en nuget.org un paquete que apunta a una version que aun no existe, y')
$line.Add('el consumidor falla al restaurar con NU1101 o NU1102.')
$line.Add('')
$line.Add('Las dependencias externas no aparecen aqui: ya estan publicadas y no imponen orden.')
$line.Add('')

foreach ($level in ($package.Values | ForEach-Object { $_.Level } | Sort-Object -Unique)) {
    $member = @($package.Values | Where-Object { $_.Level -eq $level } | Sort-Object Id)

    $heading = if ($level -eq 1) {
        "## Nivel $level - sin dependencias de la casa"
    }
    else {
        "## Nivel $level - dependen del nivel $($level - 1)"
    }

    $line.Add($heading)
    $line.Add('')
    $line.Add('| Paquete | Version | Depende de |')
    $line.Add('| --- | --- | --- |')

    foreach ($entry in $member) {
        $dependency = if ($entry.Dependencies.Count -eq 0) { '-' } else { ($entry.Dependencies -join '<br>') }
        $line.Add("| ``$($entry.Id)`` | $($entry.Version) | $dependency |")
    }

    $line.Add('')
}

$line.Add('---')
$line.Add('')
$line.Add('Antes de publicar, `eng/Test-PublishReadiness.ps1` comprueba que el suelo declarado de')
$line.Add('cada dependencia exista ya en nuget.org, que es la verificacion de que este orden se')
$line.Add('respeto.')

$content = ($line -join "`r`n") + "`r`n"

if ($Check) {
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "No existe '$Path'. Ejecuta eng/New-PublishOrder.ps1 para generarlo." -ForegroundColor Red
        exit 1
    }

    if ((Get-Content -LiteralPath $Path -Raw) -ne $content) {
        Write-Host "'$Path' esta desactualizado respecto al grafo de src/. Vuelve a ejecutar eng/New-PublishOrder.ps1." -ForegroundColor Red
        exit 1
    }

    Write-Host "'$Path' esta al dia." -ForegroundColor Green
    exit 0
}

$directory = Split-Path -Parent $Path
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }

Set-Content -LiteralPath $Path -Value $content -NoNewline
Write-Host "Escrito: $Path" -ForegroundColor Green
Write-Host "$($package.Count) paquetes en $(($package.Values | ForEach-Object { $_.Level } | Sort-Object -Unique).Count) nivel(es)."
