#Requires -Version 7.0

<#
.SYNOPSIS
    Verifica que los paquetes generados se puedan publicar sin romper a sus consumidores.

.DESCRIPTION
    Desde que las dependencias entre paquetes de la casa van por <ProjectReference>, la
    version que viaja al .nuspec la decide 'dotnet pack' leyendo el <VersionPrefix> del
    proyecto vecino en disco. Nunca consulta nuget.org, asi que un paquete puede quedar
    declarando una dependencia que alli no existe, o peor, una que existe con otro
    contenido. Este script detecta ambos casos antes del push.

    Comprobacion 1 - Dependencias resolubles
        Cada dependencia declarada en un .nuspec existe ya en nuget.org, o se publica en
        este mismo lote. Si no, el consumidor falla al restaurar con NU1101 o NU1102.

    Comprobacion 2 - Contenido sin deriva
        Si la version local de un paquete ya esta publicada, lo que se generaria ahora debe
        coincidir con lo que hay en el feed. Se comparan dos dimensiones:

        - API publica, leida del .xml de documentacion que viaja en lib/. Es un inventario
          fiel porque el repositorio compila con TreatWarningsAsErrors y no suprime CS1591,
          de modo que todo miembro publico esta documentado. Si difiere, se cambio el codigo
          sin subir la version: el consumidor restaura sin quejarse y revienta en ejecucion.

        - Dependencias declaradas en el .nuspec, calificadas por framework de destino. Esta
          dimension se desincroniza sola: con <ProjectReference> el suelo lo recalcula
          'dotnet pack' desde el proyecto vecino, asi que publicar una version de un paquete
          base cambia el .nuspec de todos sus consumidores sin que nadie edite un .csproj.

        No detecta cambios que solo alteran la implementacion sin tocar ninguna de las dos.

.PARAMETER PackageDirectory
    Carpeta con los .nupkg a verificar. Si se omite, el script ejecuta 'dotnet pack' de la
    solucion en una carpeta temporal.

.PARAMETER Configuration
    Configuracion usada al empaquetar cuando el script hace el pack. Release por defecto.

.PARAMETER PublishOnly
    Identificadores de los paquetes que realmente vas a publicar. Una dependencia se da por
    buena sin consultar el feed solo si esta en esta lista, porque se publicara junto al que
    la declara. Si se omite, el script asume que publicas el lote completo; verifica ahi que
    esa suposicion sea cierta, o el resultado dara una confianza que no corresponde.

.PARAMETER DependencyPrefix
    Prefijos de los paquetes cuyas dependencias se verifican. Por defecto solo los de la
    casa, que son los que dependen de <ProjectReference>. Los externos llegan por Central
    Package Management y ya estan publicados por definicion.

.PARAMETER IncludeExternalDependencies
    Verifica tambien las dependencias que no casan con DependencyPrefix.

.PARAMETER SkipContentComparison
    Omite la comprobacion 2, que descarga paquetes de nuget.org.

.PARAMETER FlatContainer
    Endpoint del recurso PackageBaseAddress del feed. Cambialo para verificar contra un
    feed privado.

.EXAMPLE
    ./eng/Test-PublishReadiness.ps1

.EXAMPLE
    ./eng/Test-PublishReadiness.ps1 -PackageDirectory ./artifacts -SkipContentComparison
#>

[CmdletBinding()]
param(
    [string]   $PackageDirectory,
    [string]   $Configuration = 'Release',
    [string[]] $PublishOnly,
    [string[]] $DependencyPrefix = @('Persiltech.'),
    [switch]   $IncludeExternalDependencies,
    [switch]   $SkipContentComparison,
    [string]   $FlatContainer = 'https://api.nuget.org/v3-flatcontainer'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Failures = [System.Collections.Generic.List[string]]::new()
$script:Warnings = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param([string] $Message)
    $script:Failures.Add($Message)
    Write-Host "    [ERROR]  $Message" -ForegroundColor Red
}

function Add-Warning {
    param([string] $Message)
    $script:Warnings.Add($Message)
    Write-Host "    [AVISO]  $Message" -ForegroundColor Yellow
}

function Write-Pass {
    param([string] $Message)
    Write-Host "    [OK]     $Message" -ForegroundColor Green
}

function Write-Info {
    param([string] $Message)
    Write-Host "    [.]      $Message" -ForegroundColor DarkGray
}

function Write-Heading {
    param([string] $Message)
    Write-Host ''
    Write-Host $Message -ForegroundColor Cyan
    Write-Host ('-' * $Message.Length) -ForegroundColor Cyan
}

function Open-Package {
    param([string] $Path)
    [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
}

function Read-ZipEntryText {
    param([System.IO.Compression.ZipArchiveEntry] $Entry)
    $stream = $Entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new($stream)
        try { $reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    finally { $stream.Dispose() }
}

# El .nuspec vive en la raiz del .nupkg y su namespace cambia entre versiones del esquema,
# de ahi el XPath por local-name() en lugar de un XmlNamespaceManager.
function Get-PackageManifest {
    param([string] $Path)

    $archive = Open-Package -Path $Path
    try {
        $entry = $archive.Entries |
            Where-Object { $_.FullName -notmatch '/' -and $_.FullName -like '*.nuspec' } |
            Select-Object -First 1

        if (-not $entry) { throw "El paquete '$Path' no contiene un .nuspec en su raiz." }

        [xml] $document = Read-ZipEntryText -Entry $entry

        # Las dependencias cuelgan de un <group targetFramework="..."> salvo en paquetes de
        # esquema antiguo, donde van sueltas bajo <dependencies>. Se contemplan ambos.
        $dependencies = foreach ($node in $document.SelectNodes("//*[local-name()='dependency']")) {
            $parent = $node.ParentNode
            $targetFramework = if ($parent -and $parent.LocalName -eq 'group') {
                $parent.GetAttribute('targetFramework')
            }
            else { '' }

            [pscustomobject]@{
                Id              = $node.GetAttribute('id')
                VersionRange    = $node.GetAttribute('version')
                TargetFramework = $targetFramework
            }
        }

        [pscustomobject]@{
            Id           = $document.SelectSingleNode("//*[local-name()='id']").InnerText
            Version      = $document.SelectSingleNode("//*[local-name()='version']").InnerText
            Dependencies = @($dependencies)
            Path         = $Path
        }
    }
    finally { $archive.Dispose() }
}

# Inventario de la API publica: cada miembro documentado, calificado con su framework de
# destino para que perder un TFM cuente como cambio de superficie.
function Get-PublicApi {
    param([string] $Path)

    $archive = Open-Package -Path $Path
    try {
        $members = [System.Collections.Generic.List[string]]::new()

        foreach ($entry in $archive.Entries) {
            if ($entry.FullName -notlike 'lib/*' -or $entry.FullName -notlike '*.xml') { continue }

            $targetFramework = $entry.FullName.Split('/')[1]
            [xml] $document = Read-ZipEntryText -Entry $entry

            foreach ($member in $document.SelectNodes("//*[local-name()='member']")) {
                $members.Add("$targetFramework $($member.GetAttribute('name'))")
            }
        }

        , @($members | Sort-Object -Unique)
    }
    finally { $archive.Dispose() }
}

# Dependencias en forma comparable, calificadas por framework de destino igual que la API.
# Se omite a proposito el atributo 'exclude': lo emite el SDK y puede variar entre versiones
# sin que el paquete haya cambiado, lo que produciria diferencias falsas.
function Format-Dependency {
    param([object[]] $Dependency)

    , @($Dependency | ForEach-Object {
        $targetFramework = if ($_.TargetFramework) { $_.TargetFramework } else { '(sin grupo)' }
        "$targetFramework $($_.Id) $($_.VersionRange)"
    } | Sort-Object -Unique)
}

# Devuelve $null cuando el identificador no existe en el feed, distinguiendo asi el caso
# NU1101 (paquete desconocido) del NU1102 (paquete conocido, version ausente).
function Get-PublishedVersion {
    param([string] $Id)

    $uri = "$FlatContainer/$($Id.ToLowerInvariant())/index.json"
    try {
        $response = Invoke-RestMethod -Uri $uri -TimeoutSec 30
        return @($response.versions)
    }
    catch {
        $response = $_.Exception.Response
        if ($response -and [int] $response.StatusCode -eq 404) { return $null }
        throw "No se pudo consultar '$uri'. $($_.Exception.Message)"
    }
}

function Save-PublishedPackage {
    param([string] $Id, [string] $Version, [string] $Destination)

    $identifier = $Id.ToLowerInvariant()
    $normalized = $Version.ToLowerInvariant()
    $uri = "$FlatContainer/$identifier/$normalized/$identifier.$normalized.nupkg"
    $path = Join-Path $Destination "$identifier.$normalized.nupkg"

    Invoke-WebRequest -Uri $uri -OutFile $path -TimeoutSec 120 | Out-Null
    $path
}

# Suelo de un intervalo de versiones de NuGet. '1.2.3' y '[1.2.3,2.0)' declaran 1.2.3;
# '(,2.0)' no declara suelo y devuelve cadena vacia.
function Get-MinimumVersion {
    param([string] $VersionRange)

    $value = $VersionRange.Trim()
    if ($value -match '^[\[\(]\s*([^,\]\)]*)') { return $Matches[1].Trim() }
    $value
}

function Test-VersionPublished {
    param([string] $Version, [string[]] $PublishedVersion)

    $null -ne ($PublishedVersion | Where-Object { $_ -eq $Version })
}

function Test-DependencyInScope {
    param([string] $Id)

    if ($IncludeExternalDependencies) { return $true }
    foreach ($prefix in $DependencyPrefix) {
        if ($Id.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    $false
}

# Orden de publicacion: las hojas del grafo primero, porque el .nuspec de un consumidor
# apunta a la version que su dependencia tenga al empaquetar y esa version ya debe existir.
function Get-PublishOrder {
    param([object[]] $Manifest)

    $pending = [System.Collections.Generic.List[object]]::new()
    $Manifest | ForEach-Object { $pending.Add($_) }

    $ordered = [System.Collections.Generic.List[object]]::new()

    while ($pending.Count -gt 0) {
        $pendingId = [System.Collections.Generic.HashSet[string]]::new(
            [string[]] @($pending | ForEach-Object { $_.Id }),
            [System.StringComparer]::OrdinalIgnoreCase)

        # Un paquete esta listo cuando ninguna de sus dependencias sigue en la cola.
        $ready = @($pending | Where-Object {
            $package = $_
            $blocking = @($package.Dependencies | Where-Object {
                $_.Id -ne $package.Id -and $pendingId.Contains($_.Id)
            })
            $blocking.Count -eq 0
        })

        if ($ready.Count -eq 0) { break }

        foreach ($package in $ready) {
            $ordered.Add($package)
            [void] $pending.Remove($package)
        }
    }

    # Un ciclo entre paquetes no puede publicarse en ningun orden; se emiten al final para
    # que el informe los muestre en lugar de perderlos en silencio.
    foreach ($package in $pending) { $ordered.Add($package) }

    , @($ordered)
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "publish-readiness-$([guid]::NewGuid().ToString('n').Substring(0, 8))"
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

try {
    if (-not $PackageDirectory) {
        Write-Heading 'Empaquetando la solucion'
        $PackageDirectory = Join-Path $temporaryRoot 'packages'
        New-Item -ItemType Directory -Path $PackageDirectory | Out-Null

        $output = & dotnet pack $repositoryRoot --configuration $Configuration --output $PackageDirectory --nologo --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            $output | ForEach-Object { Write-Host $_ }
            throw "'dotnet pack' fallo con codigo $LASTEXITCODE."
        }
        Write-Pass "Paquetes generados en $PackageDirectory"
    }

    $packageFile = @(
        Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' -File |
            Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    )

    if ($packageFile.Count -eq 0) { throw "No se encontro ningun .nupkg en '$PackageDirectory'." }

    $manifest = @($packageFile | ForEach-Object { Get-PackageManifest -Path $_.FullName })

    # Solo los paquetes de este conjunto eximen a una dependencia de existir ya en el feed.
    $publishSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    if ($PublishOnly) {
        foreach ($id in $PublishOnly) { [void] $publishSet.Add($id) }

        $unknown = @($PublishOnly | Where-Object { $identifier = $_; -not ($manifest | Where-Object { $_.Id -eq $identifier }) })
        if ($unknown) { throw "PublishOnly nombra paquetes que no estan en '$PackageDirectory': $($unknown -join ', ')." }
    }
    else {
        foreach ($package in $manifest) { [void] $publishSet.Add($package.Id) }
    }

    $localVersion = @{}
    foreach ($package in $manifest) { $localVersion[$package.Id] = $package.Version }

    # Solo se examina lo que se va a publicar. Un paquete que no se publica puede declarar
    # sin consecuencia una dependencia todavia ausente del feed.
    $underTest = @($manifest | Where-Object { $publishSet.Contains($_.Id) } | Sort-Object Id)

    Write-Heading "Comprobacion 1 - Dependencias resolubles ($($underTest.Count) paquetes)"

    if ($PublishOnly) {
        Write-Info "Se publicaran $($underTest.Count) de los $($manifest.Count) paquetes del lote."
    }
    else {
        Write-Info "Sin -PublishOnly: se asume que publicas los $($manifest.Count) paquetes del lote."
    }

    foreach ($package in $underTest) {
        Write-Host "  $($package.Id) $($package.Version)"

        $inScope = @($package.Dependencies | Where-Object { Test-DependencyInScope -Id $_.Id })
        if ($inScope.Count -eq 0) {
            Write-Info 'Sin dependencias en el ambito verificado.'
            continue
        }

        foreach ($dependency in $inScope) {
            $required = Get-MinimumVersion -VersionRange $dependency.VersionRange

            if (-not $required) {
                Add-Warning "$($dependency.Id) se declara sin suelo de version ('$($dependency.VersionRange)')."
                continue
            }

            if ($publishSet.Contains($dependency.Id) -and
                $localVersion.ContainsKey($dependency.Id) -and
                $localVersion[$dependency.Id] -eq $required) {
                Write-Pass "$($dependency.Id) $required se publica en este mismo lote."
                continue
            }

            $published = Get-PublishedVersion -Id $dependency.Id

            if ($null -eq $published) {
                Add-Failure "$($dependency.Id) no existe en el feed. El consumidor fallara con NU1101."
                continue
            }

            if (Test-VersionPublished -Version $required -PublishedVersion $published) {
                Write-Pass "$($dependency.Id) $required ya esta publicada."
                continue
            }

            $latest = ($published | Select-Object -Last 1)
            Add-Failure "$($dependency.Id) $required no esta publicada (la ultima es $latest). El consumidor fallara con NU1102."
        }
    }

    if (-not $SkipContentComparison) {
        Write-Heading 'Comprobacion 2 - Contenido sin deriva'

        $downloadDirectory = Join-Path $temporaryRoot 'published'
        New-Item -ItemType Directory -Path $downloadDirectory | Out-Null

        foreach ($package in $underTest) {
            Write-Host "  $($package.Id) $($package.Version)"

            $published = Get-PublishedVersion -Id $package.Id
            if ($null -eq $published) {
                Write-Pass 'Paquete nuevo, aun sin publicar.'
                continue
            }

            if (-not (Test-VersionPublished -Version $package.Version -PublishedVersion $published)) {
                Write-Pass "Version nueva. La ultima publicada es $($published | Select-Object -Last 1)."
                continue
            }

            $publishedPath = Save-PublishedPackage -Id $package.Id -Version $package.Version -Destination $downloadDirectory
            $publishedManifest = Get-PackageManifest -Path $publishedPath

            # Dos dimensiones, porque se desincronizan por separado. La API cambia al tocar
            # el codigo; las dependencias cambian solas, sin que nadie edite el .csproj, en
            # cuanto <ProjectReference> recalcula el suelo desde el proyecto vecino.
            $surface = @(
                [pscustomobject]@{
                    Name      = 'API publica'
                    Published = Get-PublicApi -Path $publishedPath
                    Local     = Get-PublicApi -Path $package.Path
                }
                [pscustomobject]@{
                    Name      = 'Dependencias declaradas'
                    Published = Format-Dependency -Dependency $publishedManifest.Dependencies
                    Local     = Format-Dependency -Dependency $package.Dependencies
                }
            )

            $divergent = [System.Collections.Generic.List[string]]::new()

            foreach ($dimension in $surface) {
                $difference = @(Compare-Object -ReferenceObject $dimension.Published -DifferenceObject $dimension.Local)
                if ($difference.Count -eq 0) { continue }

                $divergent.Add($dimension.Name)
                $dimension | Add-Member -NotePropertyName Difference -NotePropertyValue $difference
            }

            if ($divergent.Count -eq 0) {
                Add-Warning "$($package.Version) ya esta publicada y es identica. No hay nada que publicar."
                continue
            }

            Add-Failure "$($package.Version) ya esta publicada con otro contenido ($($divergent -join ' y ')). Sube la version antes de publicar."

            foreach ($dimension in $surface) {
                if (-not $dimension.PSObject.Properties['Difference']) { continue }

                Write-Host "               $($dimension.Name):" -ForegroundColor Red
                foreach ($entry in $dimension.Difference) {
                    $marker = if ($entry.SideIndicator -eq '=>') { 'solo local    ' } else { 'solo publicado' }
                    Write-Host "                 $marker  $($entry.InputObject)" -ForegroundColor Red
                }
            }
        }
    }

    Write-Heading 'Orden de publicacion'
    $order = Get-PublishOrder -Manifest $underTest
    $position = 1
    foreach ($package in $order) {
        Write-Host ('  {0,2}. {1} {2}' -f $position, $package.Id, $package.Version)
        $position++
    }

    Write-Heading 'Resumen'
    if ($script:Warnings.Count -gt 0) {
        Write-Host "  $($script:Warnings.Count) aviso(s)." -ForegroundColor Yellow
    }

    if ($script:Failures.Count -gt 0) {
        Write-Host "  $($script:Failures.Count) error(es). No publiques hasta resolverlos." -ForegroundColor Red
        exit 1
    }

    Write-Host '  Todo listo para publicar.' -ForegroundColor Green
    exit 0
}
finally {
    Remove-Item -Path $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
}
