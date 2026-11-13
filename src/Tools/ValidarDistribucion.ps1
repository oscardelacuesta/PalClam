# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot,[ValidateSet('all','win-x64','win-x86')][string]$RuntimeId='all',[switch]$PayloadOnly,[switch]$RequirePortable)
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
# .NET Framework / Windows PowerShell 5.1 does not load both assemblies implicitly.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root=[IO.Path]::GetFullPath($ProjectRoot);$version=Get-PalClamVersion
$rids=if ($RuntimeId -eq 'all') { @(Get-ReleaseRids) } else { @($RuntimeId) }
try {
    & (Join-Path $palClamToolsDirectory 'PrepararLicencias.ps1') -ProjectRoot $root -VerifyOnly
    if ($LASTEXITCODE -ne 0) { throw 'No se puede distribuir sin licencias y fuentes de dependencias.' }
    & (Join-Path $palClamToolsDirectory 'VerificarGPL3.ps1') -ProjectRoot $root -Published -RuntimeId $RuntimeId
    if ($LASTEXITCODE -ne 0) { throw 'Publicacion sin licencia GPLv3 verificada.' }
    $records=@();$database=Join-Path $root 'ThirdParty\OfflineSignatures';$found=@()
    foreach ($family in @('main','daily','bytecode')) {
        $db=@('cld','cvd') | ForEach-Object { Join-Path $database ($family+'.'+$_) } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if (-not $db -or (Get-Item -LiteralPath $db).Length -lt 1024) { throw "Falta base $family completa." }
        $found+=@($db)
    }
    foreach ($rid in $rids) {
        $app=Join-Path $root ('PalClam\dist\'+$rid+'\PalClam.exe')
        $service=Join-Path $root ('PalClam.Service\dist\'+$rid+'\PalClam.Service.exe')
        Assert-PeArchitecture $app $rid;Assert-PeArchitecture $service $rid
        $engine=Join-Path $root ('ThirdParty\ClamAV\'+$rid)
        foreach ($name in @('clamscan.exe','freshclam.exe','sigtool.exe')) { Assert-PeArchitecture (Join-Path $engine $name) $rid }
        $metadata=Get-EngineMetadata $rid
        $archive=Join-Path $root ('Tools\cache\'+$metadata.Asset)
        if (-not (Test-Path -LiteralPath $archive) -or (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $metadata.Sha256) { throw "Motor $rid sin archivo de origen verificado." }
        # Comparar la preparación con los bytes del ZIP firmado/fijado: no aceptar un EXE sustituido después de extraerlo.
        $zip=[System.IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                $relative=$entry.FullName.Replace('\','/')
                if ($relative.StartsWith($metadata.Root+'/')) { $relative=$relative.Substring($metadata.Root.Length+1) }
                if (-not $entry.Name) { continue }
                if ($relative.Split('/') -contains '..') { throw 'Ruta de ZIP no permitida.' }
                $target=Join-Path $engine $relative
                if (-not (Test-Path -LiteralPath $target)) { throw "Falta archivo del motor: $relative" }
                $stream=$entry.Open();$hash=[Security.Cryptography.SHA256]::Create()
                try { $expected=([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','') } finally { $stream.Dispose();$hash.Dispose() }
                if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) { throw "Motor modificado: $target" }
            }
        } finally { $zip.Dispose() }
        $records+=@([pscustomobject]@{ Runtime=$rid; Application=$app; Service=$service; EngineSha256=$metadata.Sha256; Pe='OK' })
    }
    $source=Join-Path $root 'ThirdParty\SourcePackages\clamav-1.5.4.tar.gz'
    if (-not (Test-Path -LiteralPath $source) -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne '1af1117a228f1b5bc7fa91a0dabc37848a99e7d25188e9be8043332ce721dfd3') { throw 'Falta fuente ClamAV correspondiente y verificada.' }
    if (-not (Test-Path -LiteralPath (Join-Path $root ('dist\source\PalClam_'+$version+'_Source.zip')))) { throw 'Falta el ZIP de fuentes PalClam.' }
    foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md')) { if (-not (Test-Path -LiteralPath (Join-Path $root $name))) { throw "Falta $name" } }
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        $native=if ([Environment]::Is64BitOperatingSystem) { 'win-x64' } else { 'win-x86' }
        $engine=Join-Path $root ('ThirdParty\ClamAV\'+$native)
        if (-not (Test-Path -LiteralPath (Join-Path $engine 'sigtool.exe'))) { throw 'Falta el motor ejecutable en el sistema de compilacion.' }
        Invoke-Checked (Join-Path $engine 'clamscan.exe') @('--version')
        $config=Join-Path ([IO.Path]::GetTempPath()) ('PalClam-Validate-'+[guid]::NewGuid().ToString('N')+'.conf')
        $testFile=$config+'.txt'
        try {
            $normal=$database.Replace('\','/')
            if ($normal.Contains('"') -or $normal.Contains("`n") -or $normal.Contains("`r")) { throw 'Ruta de firmas no admitida.' }
            [IO.File]::WriteAllText($config,('DatabaseDirectory "'+$normal+'"'+"`r`nDatabaseMirror database.clamav.net`r`n"),(New-Object Text.UTF8Encoding($false)))
            Invoke-Checked (Join-Path $engine 'freshclam.exe') @(('--config-file='+$config),'--version')
            foreach ($db in $found) { Invoke-Checked (Join-Path $engine 'sigtool.exe') @('--info',$db) }
            [IO.File]::WriteAllText($testFile,'PalClam: harmless file for offline scanner validation.')
            Invoke-Checked (Join-Path $engine 'clamscan.exe') @(('--database='+$database),'--no-summary','--stdout',$testFile)
        } finally {
            foreach ($file in @($config,$testFile)) { if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force } }
        }
    }
    if ($PayloadOnly) { Write-Host '[OK] Payloads, motores, fuentes y firmas validados. No se ejecuta el motor de arquitectura incompatible con el host.';exit 0 }
    $artifacts=@()
    foreach ($rid in $rids) {
        # Salidas de arquitectura explicitas: PalClam_1.2.2_Setup_win-x64 y PalClam_1.2.2_Setup_win-x86.
        $path=Join-Path $root ('dist\installer\PalClam_'+$version+'_Setup_'+$rid+'.exe')
        if (-not (Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -lt 1000000) { throw "Falta instalador: $path" }
        $artifacts+=@($path)
        $portable=Join-Path $root ('dist\portable\PalClam_'+$version+'_Portable_'+$rid+'.zip')
        if (Test-Path -LiteralPath $portable) { $artifacts+=@($portable) } elseif ($RequirePortable) { throw "Falta portable: $portable" }
    }
    if ($RuntimeId -eq 'all') {
        $universal=Join-Path $root ('dist\installer\PalClam_'+$version+'_Setup.exe')
        if (-not (Test-Path -LiteralPath $universal) -or (Get-Item -LiteralPath $universal).Length -lt 1000000) { throw 'Falta el instalador automatico.' }
        $artifacts+=@($universal)
    }
    $checksums=@($artifacts | ForEach-Object { [pscustomobject]@{ File=[IO.Path]::GetFileName($_); Bytes=(Get-Item -LiteralPath $_).Length; Sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() } })
    $report=[pscustomobject]@{ Version=$version; License='GPL-3.0-or-later'; PreparedUtc=[DateTime]::UtcNow.ToString('o'); Payload=$records; Files=$checksums; WindowsAcceptance='PENDIENTE'; Note='Validacion de preparacion. No publica ni certifica los ensayos manuales.' }
    New-Item -ItemType Directory -Force -Path (Join-Path $root 'dist') | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $root ('dist\RESULTADOS_'+$version+'.json')) -Encoding UTF8
    Write-Host '[OK] Salidas comprobadas. La aceptacion de interfaz, Outlook e instalacion sigue siendo manual.' -ForegroundColor Green
    exit 0
} catch { Write-Host ('[ERROR] '+$_.Exception.Message) -ForegroundColor Red;exit 1 }
