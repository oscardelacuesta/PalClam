# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([ValidateSet('win-x64','win-x86')][string]$RuntimeId='win-x64',[string]$ProjectRoot)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
$ErrorActionPreference='Stop'
Import-Module (Join-Path $palClamToolsDirectory 'Distribution.psm1') -Force -DisableNameChecking
# .NET Framework / Windows PowerShell 5.1 does not load both assemblies implicitly.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
try {
    & (Join-Path $palClamToolsDirectory 'PrepararLicencias.ps1') -ProjectRoot $ProjectRoot -VerifyOnly
    if ($LASTEXITCODE -ne 0) { throw 'No se empaqueta una portable sin las licencias verificadas.' }
    $version=Get-PalClamVersion
    $app=Join-Path $ProjectRoot ('PalClam\dist\'+$RuntimeId)
    $engine=Join-Path $ProjectRoot ('ThirdParty\ClamAV\'+$RuntimeId)
    Assert-PeArchitecture (Join-Path $app 'PalClam.exe') $RuntimeId
    foreach ($file in @('clamscan.exe','freshclam.exe','sigtool.exe')) { Assert-PeArchitecture (Join-Path $engine $file) $RuntimeId }
    $source=Join-Path $ProjectRoot ('dist\source\PalClam_'+$version+'_Source.zip')
    if (-not (Test-Path -LiteralPath $source)) { throw 'Falta el ZIP de fuentes correspondientes.' }
    $workRelative='dist\portable-staging\'+$RuntimeId
    Remove-BuildFolder $ProjectRoot $workRelative
    $work=Join-Path $ProjectRoot $workRelative
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    Get-ChildItem -LiteralPath $app -Force | Where-Object { $_.Name -notin @('Data','palclam.portable') -and $_.Extension -ne '.pdb' } | Copy-Item -Destination $work -Recurse -Force
    New-Item -ItemType Directory -Force -Path (Join-Path $work 'ClamAV'),(Join-Path $work 'OfflineSignatures'),(Join-Path $work 'Source'),(Join-Path $work 'Data') | Out-Null
    Get-ChildItem -LiteralPath $engine -Force | Copy-Item -Destination (Join-Path $work 'ClamAV') -Recurse -Force
    foreach ($family in @('main','daily','bytecode')) {
        $db=@('cld','cvd') | ForEach-Object { Join-Path $ProjectRoot ('ThirdParty\OfflineSignatures\'+$family+'.'+$_) } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if (-not $db -or (Get-Item -LiteralPath $db).Length -lt 1024) { throw "Faltan firmas verificadas: $family" }
        Copy-Item -LiteralPath $db -Destination (Join-Path $work 'OfflineSignatures')
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $work 'Source')
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ThirdParty\SourcePackages\clamav-1.5.4.tar.gz') -Destination (Join-Path $work 'Source')
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'dist\legal') -Destination (Join-Path $work 'Licenses') -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ThirdParty\SourcePackages\Libraries') -Destination (Join-Path $work 'Source\Libraries') -Recurse -Force
    foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $ProjectRoot $name) -Destination $work }
    New-Item -ItemType Directory -Force -Path (Join-Path $work 'Source\docs') | Out-Null
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $work 'Source')
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'docs\TERCEROS-Y-LEGAL.md') -Destination (Join-Path $work 'Source\docs')
    Set-Content -LiteralPath (Join-Path $work 'palclam.portable') -Value ('PalClam '+$version+' '+$RuntimeId) -Encoding Ascii
    @(('PALCLAM '+$version+' PORTABLE '+$RuntimeId),
      'Web del producto: https://palclam.com/ | Autor: Oscar de la Cuesta - palentino.es',
      'Extrae TODO el ZIP en una carpeta con permiso de escritura y ejecuta PalClam.exe.',
      'No se instala .NET: el runtime y el motor de la arquitectura adecuada estan incluidos.',
      'Los datos propios se guardan en Data. No lo ejecutes directamente desde el ZIP.',
      'No registra servicios, tareas, inicio automatico ni menu de Explorer.',
      'Las claves IA cifradas por DPAPI solo funcionan con el usuario Windows que las guardo.',
      'Mantener copias de Data: puede contener historial privado, rutas y cuarentena.',
      'Para actualizar, cierra PalClam y sustituye el programa conservando Data.',
      'El sistema operativo y sus dependencias pueden crear caches y registros normales.',
      'El monitor anti-ransomware es reactivo; no bloquea ni descifra archivos.',
      'Licencias y avisos de terceros en Licenses. Fuentes correspondientes en Source.') | Set-Content -LiteralPath (Join-Path $work 'LEEME_PORTABLE.txt') -Encoding UTF8
    $destination=Join-Path $ProjectRoot ('dist\portable\PalClam_'+$version+'_Portable_'+$RuntimeId+'.zip')
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    $partial=$destination+'.partial'
    if (Test-Path -LiteralPath $partial) { Remove-Item -LiteralPath $partial -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($work,$partial,[System.IO.Compression.CompressionLevel]::Optimal,$false)
    Move-Item -LiteralPath $partial -Destination $destination -Force
    $check=[System.IO.Compression.ZipFile]::OpenRead($destination)
    try {
        $names=@($check.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
        foreach ($required in @('PalClam.exe','palclam.portable','ClamAV/clamscan.exe','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md','Licenses/DEPENDENCIES.json',('Source/PalClam_'+$version+'_Source.zip'))) {
            if ($names -notcontains $required) { throw "Portable incompleto: $required" }
        }
        if (@($names | Where-Object { $_ -like 'Licenses/Runtime/*' }).Count -lt 2) { throw 'Portable sin licencias del runtime.' }
        foreach ($legal in (Get-Content -LiteralPath (Join-Path $ProjectRoot 'Tools\licenses-lock.json') -Encoding UTF8 -Raw | ConvertFrom-Json).packages) {
            foreach ($entry in $legal.legal_files) {
                $required='Licenses/'+$legal.id+'-'+$legal.version+'/'+$entry.file
                if ($names -notcontains $required) { throw "Portable sin aviso legal: $required" }
            }
            if ($legal.PSObject.Properties.Name -contains 'corresponding_source') {
                $required='Source/Libraries/'+[IO.Path]::GetFileName($legal.corresponding_source.file)
                if ($names -notcontains $required) { throw "Portable sin fuentes: $required" }
            }
        }
        if (@($names | Where-Object { $_ -like 'Data/*' -and $_ -notmatch '/$' }).Count -gt 0) { throw 'No se distribuyen datos personales.' }
    } finally { $check.Dispose() }
    Write-Host "[OK] Portable creado: $destination"
    exit 0
} catch { Write-Host ('[ERROR] '+$_.Exception.Message) -ForegroundColor Red; exit 1 }
