# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot,[switch]$VerifyOnly)
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
Import-Module (Join-Path $palClamToolsDirectory 'Distribution.psm1') -Force -DisableNameChecking
try {
    $root=[IO.Path]::GetFullPath($ProjectRoot)
    $lock=Get-Content -LiteralPath (Join-Path $root 'Tools\licenses-lock.json') -Encoding UTF8 -Raw | ConvertFrom-Json
    if ($lock.project_license.spdx -cne 'GPL-3.0-or-later') { throw 'Licencia propia no corresponde a GPLv3 o posterior.' }
    foreach ($name in @('COPYING',$lock.project_license.file)) {
        $path=Join-Path $root $name
        if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $lock.project_license.sha256) { throw "Texto GPLv3 ausente o modificado: $name" }
    }
    $reviewed=@($lock.packages | ForEach-Object { $_.id+'/'+$_.version })
    $resolved=@();$assetsList=@()
    foreach ($project in @('PalClam','PalClam.Service')) {
        $path=Join-Path $root ($project+'\obj\project.assets.json')
        if (-not (Test-Path -LiteralPath $path)) { throw "Compila antes de preparar licencias: falta $path" }
        $assets=Get-Content -LiteralPath $path -Encoding UTF8 -Raw | ConvertFrom-Json
        $assetsList+=@($assets)
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            $used=$false
            foreach ($target in $assets.targets.PSObject.Properties) {
                $item=$target.Value.PSObject.Properties[$library.Name]
                if ($item -and @($item.Value.PSObject.Properties.Name | Where-Object { $_ -in @('runtime','native','runtimeTargets','resources','compile') }).Count -gt 0) { $used=$true }
            }
            if ($used) { $resolved+=@($library.Name) }
        }
    }
    $resolved=@($resolved | Sort-Object -Unique)
    foreach ($package in $resolved) { if ($reviewed -notcontains $package) { throw "Dependencia sin licencia revisada: $package. Actualiza el inventario y sus avisos antes de distribuir." } }
    foreach ($package in $reviewed) { if ($resolved -notcontains $package) { throw "Inventario desactualizado: $package no pertenece a la compilacion." } }
    $inno=Join-Path $root 'ThirdParty\Licenses\Inno-Setup-LICENSE.txt'
    if (-not (Test-Path -LiteralPath $inno) -or (Get-FileHash -LiteralPath $inno -Algorithm SHA256).Hash.ToLowerInvariant() -ne $lock.inno_license_sha256) { throw 'Falta la licencia original verificada de Inno Setup.' }
    $destination=Join-Path $root 'dist\legal'
    if (-not $VerifyOnly) {
        Remove-BuildFolder $root 'dist\legal'
        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        Get-ChildItem -LiteralPath (Join-Path $root 'ThirdParty\Licenses') -Force | Copy-Item -Destination $destination -Recurse -Force
        foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $destination }
        Copy-Item -LiteralPath (Join-Path $root 'Tools\licenses-lock.json') -Destination (Join-Path $destination 'DEPENDENCIES.json')
    }
    foreach ($entry in $lock.packages) {
        $folder=Join-Path $root ('ThirdParty\Licenses\'+$entry.id+'-'+$entry.version)
        foreach ($file in $entry.legal_files) {
            $path=Join-Path $folder $file.file
            if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Falta o se ha alterado un aviso legal: $path" }
            if ($VerifyOnly) {
                $copied=Join-Path $destination ($entry.id+'-'+$entry.version+'\'+$file.file)
                if (-not (Test-Path -LiteralPath $copied) -or (Get-FileHash -LiteralPath $copied -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Aviso no empaquetado: $copied" }
            }
        }
        if ($entry.PSObject.Properties.Name -contains 'corresponding_source') {
            $source=$entry.corresponding_source
            $path=Join-Path $root $source.file
            if ($VerifyOnly) {
                if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $source.sha256) { throw "Falta fuente correspondiente: $path" }
            } else { Get-VerifiedFile $source.url $path $source.sha256 }
        }
    }
    # Los paquetes runtime distribuidos por publish contienen sus textos originales.
    # Usar las versiones realmente resueltas por el SDK, nunca la de un runtime instalado al azar.
    $runtimeCount=0;$seen=@{}
    foreach ($assets in $assetsList) {
        $roots=@($assets.packageFolders.PSObject.Properties | ForEach-Object { $_.Name })
        $downloads=@($assets.project.frameworks.PSObject.Properties | ForEach-Object {
            if ($_.Value.PSObject.Properties.Name -contains 'downloadDependencies') { $_.Value.downloadDependencies }
        })
        $additional=@()
        foreach ($item in $downloads) {
            foreach ($rid in @('win-x64','win-x86')) {
                if ($item.name -match '^Microsoft\.(NETCore|WindowsDesktop|AspNetCore)\.App\.(Runtime|Host)\.win-(x64|x86)$' -and (Test-Path -LiteralPath (Join-Path $root ('PalClam\dist\'+$rid+'\PalClam.exe')))) {
                    $additional+=@([pscustomobject]@{ name=($item.name -replace 'win-(x64|x86)$',$rid);version=$item.version })
                }
            }
        }
        $downloads+=@($additional)
        foreach ($download in $downloads) {
            if ($download.name -notmatch '^Microsoft\.(NETCore|WindowsDesktop|AspNetCore)\.App\.(Runtime|Host)\.win-(x64|x86)$') { continue }
            $version=@($download.version -split '[\[\], ]+' | Where-Object { $_ })[0]
            if ($seen.ContainsKey($download.name+'/'+$version)) { continue };$seen[$download.name+'/'+$version]=$true
            $packagePath=$null
            foreach ($parent in $roots) {
                $candidate=Join-Path $parent ($download.name.ToLowerInvariant()+'/'+$version)
                if (Test-Path -LiteralPath $candidate) { $packagePath=$candidate;break }
            }
            if (-not $packagePath) { throw "Falta paquete runtime para recuperar sus licencias: $($download.name)/$version" }
            $files=@(Get-ChildItem -LiteralPath $packagePath -File | Where-Object { $_.Name -match '(?i)(license|notice|copying|copyright)' })
            if (@($files | Where-Object { $_.Name -match '(?i)license' }).Count -eq 0) { throw "Runtime sin licencia original: $packagePath" }
            $target=Join-Path $destination ('Runtime\'+$download.name+'-'+$version)
            if (-not $VerifyOnly) { New-Item -ItemType Directory -Force -Path $target | Out-Null }
            foreach ($file in $files) {
                $copied=Join-Path $target $file.Name
                if ($VerifyOnly) {
                    if (-not (Test-Path -LiteralPath $copied) -or (Get-FileHash -LiteralPath $copied -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) { throw "Falta aviso runtime: $copied" }
                } else { Copy-Item -LiteralPath $file.FullName -Destination $copied }
            }
            $runtimeCount++
        }
    }
    if ($runtimeCount -lt 2) { throw 'No se han localizado las licencias del runtime .NET y Windows Desktop.' }
    foreach ($file in @('COPYING','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md','Inno-Setup-LICENSE.txt','DEPENDENCIES.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $destination $file))) { throw "Falta documento distribuible: $file" }
    }
    Write-Host ('[OK] Licencias: '+$reviewed.Count+' paquetes, runtimes reales y fuentes MPL/GPL comprobados.')
    exit 0
} catch { Write-Host ('[ERROR LICENCIAS] '+$_.Exception.Message) -ForegroundColor Red;exit 1 }
