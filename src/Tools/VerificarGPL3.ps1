# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot,[switch]$Published,[ValidateSet('all','win-x64','win-x86')][string]$RuntimeId='all')
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
try {
    $lock=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Tools\licenses-lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($lock.project_license.spdx -cne 'GPL-3.0-or-later') { throw 'SPDX propio incorrecto.' }
    foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt')) {
        $path=Join-Path $ProjectRoot $name
        if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $lock.project_license.sha256) { throw "GPLv3 oficial ausente o alterada: $name" }
    }
    $props=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Directory.Build.props') -Raw -Encoding UTF8
    if ($props -notmatch '<PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>' -or $props -notmatch 'PalClam.License.GPL3' -or $props -notmatch 'AssemblyMetadata Include="License" Value="GPL-3.0-or-later"') { throw 'Faltan metadatos o recurso de licencia compilada.' }
    $copyright=Get-Content -LiteralPath (Join-Path $ProjectRoot 'COPYRIGHT.md') -Raw -Encoding UTF8
    if ($copyright -notmatch 'GPL-3.0-or-later' -or $copyright -notmatch 'either version 3' -or $copyright -notmatch 'WITHOUT ANY WARRANTY') { throw 'Aviso propio incompleto.' }
    $count=0
    foreach ($project in @('PalClam','PalClam.Service','PalClam.SmokeTests','PalClam.ThemeTests','PalClam.LocalizationTests','PalClam.FeatureTests','PalClam.DistributionTests','Tools')) {
        $projectSourceRoot=Join-Path $ProjectRoot $project
        foreach ($file in (Get-ChildItem -LiteralPath $projectSourceRoot -Filter '*.cs' -File -Recurse | Where-Object { $_.FullName.Substring($projectSourceRoot.Length) -notmatch '[\\/](bin|obj|dist)[\\/]' })) {
            $text=Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            if ($text -notmatch 'SPDX-License-Identifier: GPL-3.0-or-later') { throw "Fuente sin declaracion GPLv3: $($file.Name)" }
            if ($project -in @('PalClam','PalClam.Service') -and ($text -match 'GPL-2.0-only' -or $text -match 'LICENSE-GPL-2.0.txt' -or $text -match '(DllImport|LibraryImport)\s*\([^)]*libclamav')) { throw "Declaracion antigua o enlace nativo ClamAV no permitido: $($file.Name)" }
            $count++
        }
    }
    if ($count -lt 1) { throw 'No se han encontrado fuentes propias para validar su licencia.' }
    foreach ($lang in @('','en','fr','de','pt','it')) {
        $suffix=if ($lang) { '.'+$lang } else { '' }
        $res=Get-Content -LiteralPath (Join-Path $ProjectRoot ('PalClam\Localization\Strings'+$suffix+'.resx')) -Raw -Encoding UTF8
        if ($res -notmatch 'GPL-3.0-or-later' -or $res -match 'GPL-2.0-only' -or $res -match 'GPLv2') { throw "Etiqueta propia de licencia antigua en idioma $lang" }
    }
    $iss=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Installer\PalClam.iss') -Raw -Encoding UTF8
    if ($iss -notmatch 'LicenseFile=\.\.\\LICENSE-GPL-3.0.txt' -or $iss -notmatch 'Source: "\.\.\\COPYING"') { throw 'El instalador no presenta y entrega GPLv3.' }
    foreach ($id in @('OpenMcdf','UTF.Unknown')) {
        $entry=@($lock.packages | Where-Object { $_.id -eq $id })[0]
        if ($entry.chosen_license -notmatch 'GPL-3.0-or-later') { throw "Seleccion de licencia antigua: $id" }
        $path=Join-Path $ProjectRoot ('ThirdParty\SourcePackages\Libraries\'+[IO.Path]::GetFileName($entry.corresponding_source.file))
        if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.corresponding_source.sha256) { throw "Fuente upstream alterada o ausente: $id" }
    }
    if ($Published) {
        $license=Get-Content -LiteralPath (Join-Path $ProjectRoot 'LICENSE-GPL-3.0.txt') -Raw -Encoding UTF8
        $rids=if ($RuntimeId -eq 'all') { @('win-x64','win-x86') } else { @($RuntimeId) }
        foreach ($rid in $rids) {
            foreach ($relative in @(('PalClam\dist\'+$rid+'\PalClam.dll'),('PalClam.Service\dist\'+$rid+'\PalClam.Service.exe'))) {
                $path=Join-Path $ProjectRoot $relative
                if (-not (Test-Path -LiteralPath $path)) { throw "Falta binario publicado: $relative" }
                $text=[Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($path))
                if (-not $text.Contains('GPL-3.0-or-later') -or -not $text.Contains('PalClam.License.GPL3') -or -not $text.Contains($license)) { throw "Binario sin metadatos o texto GPLv3 integro: $relative" }
                Write-Host ('[OK] GPLv3 embebida: '+$relative)
            }
            foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt')) {
                $path=Join-Path $ProjectRoot ('PalClam\dist\'+$rid+'\'+$name)
                if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $lock.project_license.sha256) { throw "Publicacion sin GPLv3 correspondiente: $path" }
            }
        }
    }
    Write-Host ('[OK] GPLv3: '+$count+' fuentes C#, 6 idiomas, metadatos, instalador y licencias de bibliotecas comprobados. ClamAV conserva GPLv2 como programa externo.')
    exit 0
} catch { Write-Host ('[ERROR GPL3] '+$_.Exception.Message) -ForegroundColor Red;exit 1 }
