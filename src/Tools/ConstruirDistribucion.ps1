# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
[CmdletBinding()]
param([ValidateSet('All','Build','Test','Prepare','Installer','Portable','Validate','CheckPaths')][string]$Mode='All',
      [ValidateSet('all','win-x64','win-x86')][string]$RuntimeId='all',
      [string]$ProjectRoot)
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
$root=[IO.Path]::GetFullPath($ProjectRoot)
$rids=if ($RuntimeId -eq 'all') { @(Get-ReleaseRids) } else { @($RuntimeId) }
$version=Get-PalClamVersion
function Run-Script([string]$Name,[string[]]$Extra) {
    $scriptPath=Join-Path $palClamToolsDirectory $Name
    if (-not (Test-Path -LiteralPath $scriptPath) -or (Get-Item -LiteralPath $scriptPath).Length -lt 100) { throw "Script ausente o incompleto: $Name" }
    Invoke-Checked 'powershell.exe' (@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $palClamToolsDirectory $Name))+$Extra)
}
function Find-Inno {
    $candidates=@($env:INNO_SETUP_COMPILER)
    $pathFile=Join-Path $palClamToolsDirectory 'InnoSetupCompiler.path'
    if (Test-Path -LiteralPath $pathFile) { $candidates+=@(Get-Content -LiteralPath $pathFile | Select-Object -First 1) }
    $command=Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) { $candidates+=$command.Source }
    foreach ($parent in @(${env:ProgramFiles(x86)},$env:ProgramFiles,(Join-Path $env:LOCALAPPDATA 'Programs'))) {
        if ($parent) { foreach ($major in @('6','7')) { $candidates+=Join-Path $parent ('Inno Setup '+$major+'\ISCC.exe') } }
    }
    foreach ($candidate in $candidates) { if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate } }
    throw 'No se encuentra ISCC.exe. Instala Inno Setup o configura INNO_SETUP_COMPILER con su ruta completa.'
}
try {
    if ($Mode -eq 'CheckPaths') {
        foreach ($relative in @('PalClam/PalClam.csproj','Tools/Distribution.psm1','Tools/DistributionSource.ps1')) {
            if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) { throw "Raiz de proyecto incorrecta: falta $relative" }
        }
        Write-Host ('[OK RUTA] '+$root)
        exit 0
    }
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Este proceso de distribucion se ejecuta en Windows.' }
    Remove-DeprecatedStartAlias $root
    Set-Location -LiteralPath $root
    Write-Host "PALCLAM $version - X64 + X86 + PORTABLE" -ForegroundColor Cyan
    if ($Mode -in @('All','Build','Test')) {
        if (-not (Get-Command dotnet.exe -ErrorAction SilentlyContinue)) { throw 'Instala el SDK .NET 10.' }
        $sdks=& dotnet.exe --list-sdks
        if ($LASTEXITCODE -ne 0 -or -not ($sdks -match '^10\.')) { throw 'Falta el SDK .NET 10.' }
        Run-Script 'TestSupportTests.ps1' @('-ProjectRoot',$root)
        Run-Script 'ScriptBootstrapTests.ps1' @('-ProjectRoot',$root)
        Run-Script 'VerificarGPL3.ps1' @('-ProjectRoot',$root)
        Run-Script 'VerificarFuentes.ps1' @('-ProjectRoot',$root)
        Run-Script 'DistributionPackagingTests.ps1' @('-ProjectRoot',$root)
        foreach ($test in @('PalClam.LocalizationTests','PalClam.DistributionTests','PalClam.FeatureTests','PalClam.SmokeTests','PalClam.ThemeTests')) {
            Invoke-Checked 'dotnet.exe' @('run','--project',(Join-Path $root ($test+'\'+$test+'.csproj')),'-c','Release','-p:UseSharedCompilation=false','--disable-build-servers')
        }
        Invoke-Checked 'dotnet.exe' @('run','--project',(Join-Path $root 'PalClam.Service\PalClam.Service.csproj'),'-c','Release','--','--selftest')
    }
    if ($Mode -in @('All','Build')) {
        foreach ($rid in $rids) {
            $target=if ($rid -eq 'win-x86') { 'x86' } else { 'x64' }
            foreach ($project in @('PalClam','PalClam.Service')) {
                Remove-BuildFolder $root ($project+'\dist\'+$rid)
                $publishArgs=@('publish',($project+'\'+$project+'.csproj'),'-c','Release','-r',$rid,'--self-contained','true',
                        ('-p:PlatformTarget='+$target),'-p:PublishTrimmed=false','-p:UseSharedCompilation=false','-m:1','--disable-build-servers',
                        '-o',($project+'\dist\'+$rid))
                if ($project -eq 'PalClam') { $publishArgs+=@('-p:PublishSingleFile=false') }
                else { $publishArgs+=@('-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true') }
                Invoke-Checked 'dotnet.exe' $publishArgs
                Assert-PeArchitecture (Join-Path $root ($project+'\dist\'+$rid+'\'+$project+'.exe')) $rid
            }
        }
    }
    if ($Mode -in @('All','Build')) { Run-Script 'VerificarGPL3.ps1' @('-ProjectRoot',$root,'-Published','-RuntimeId',$RuntimeId) }
    if ($Mode -in @('All','Prepare')) {
        foreach ($rid in $rids) { Run-Script 'PrepararClamAV.ps1' @('-RuntimeId',$rid) }
        $native=if ([Environment]::Is64BitOperatingSystem) { 'win-x64' } else { 'win-x86' }
        if ($rids -notcontains $native) { Run-Script 'PrepararClamAV.ps1' @('-RuntimeId',$native) }
        Run-Script 'PrepararFirmasOffline.ps1' @('-RuntimeId',$native)
    }
    if ($Mode -in @('All','Installer','Portable')) {
        Run-Script 'PrepararLicencias.ps1' @('-ProjectRoot',$root)
        Run-Script 'DistributionSource.ps1' @('-ProjectRoot',$root)
    }
    if ($Mode -in @('All','Portable')) {
        Run-Script 'ValidarDistribucion.ps1' @('-ProjectRoot',$root,'-RuntimeId',$RuntimeId,'-PayloadOnly')
        foreach ($rid in $rids) { Run-Script 'Portable.ps1' @('-RuntimeId',$rid,'-ProjectRoot',$root) }
    }
    if ($Mode -in @('All','Installer')) {
        Run-Script 'ValidarDistribucion.ps1' @('-ProjectRoot',$root,'-RuntimeId',$RuntimeId,'-PayloadOnly')
        $compiler=Find-Inno
        $compilerLicense=Join-Path (Split-Path -Parent $compiler) 'license.txt'
        if (Test-Path -LiteralPath $compilerLicense) { Copy-Item -LiteralPath $compilerLicense -Destination (Join-Path $root 'dist\legal\Inno-Setup-Compiler-LICENSE.txt') }
        $variants=@($rids | ForEach-Object { if ($_ -eq 'win-x86') { 'x86' } else { 'x64' } })
        if ($RuntimeId -eq 'all') { $variants+=@('dual') }
        foreach ($variant in $variants) {
            $suffix=if ($variant -eq 'dual') { '' } elseif ($variant -eq 'x86') { '_win-x86' } else { '_win-x64' }
            $expected=Join-Path $root ('dist\installer\PalClam_'+$version+'_Setup'+$suffix+'.exe')
            if (Test-Path -LiteralPath $expected) { Remove-Item -LiteralPath $expected -Force }
            Invoke-Checked $compiler @(('/DBuildArch='+$variant),(Join-Path $root 'Installer\PalClam.iss'))
            if (-not (Test-Path -LiteralPath $expected)) { throw "Inno Setup no genero $expected" }
        }
    }
    if ($Mode -in @('All','Validate','Installer')) {
        $validationArgs=@('-ProjectRoot',$root,'-RuntimeId',$RuntimeId)
        if ($Mode -eq 'All') { $validationArgs+=@('-RequirePortable') }
        Run-Script 'ValidarDistribucion.ps1' $validationArgs
    }
    Write-Host "[OK] $Mode completado. Salidas: dist\installer y dist\portable." -ForegroundColor Green
    exit 0
} catch { Write-Host ('[ERROR] '+$_.Exception.Message) -ForegroundColor Red; exit 1 }
