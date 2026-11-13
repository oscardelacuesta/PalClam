# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot,[string]$Destination)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
$ErrorActionPreference='Stop'
# .NET Framework / Windows PowerShell 5.1 does not load both assemblies implicitly.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not $Destination) { $Destination=Join-Path $ProjectRoot 'dist\source\PalClam_1.2.2_Source.zip' }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
$partial=$Destination+'.partial'
$allowed=@('.cs','.csproj','.xaml','.resx','.ico','.png','.gif','.ps1','.psm1','.py','.md','.txt','.json','.ini','.manifest','.sln','.slnx','.iss','.props','.targets','.nuspec','.pem')
$folders=@('PalClam','PalClam.Service','PalClam.SmokeTests','PalClam.ThemeTests','PalClam.LocalizationTests','PalClam.FeatureTests','PalClam.DistributionTests','Tools','Installer','docs','ThirdParty/Licenses','ThirdParty/SourcePackages/Libraries')
try {
    # Constructors can fail too: protect the file handle from its first acquisition.
    $stream=$null
    $archive=$null
    try {
        $stream=[IO.File]::Open($partial,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $archive=New-Object System.IO.Compression.ZipArchive($stream,[System.IO.Compression.ZipArchiveMode]::Create,$false)
        $sourceRecords=@()
        $files=@(Get-ChildItem -LiteralPath $ProjectRoot -File)+@(foreach ($name in @('ThirdParty/CHECKSUMS.txt','ThirdParty/README.md')) { Get-Item -LiteralPath (Join-Path $ProjectRoot $name) })+@(foreach ($folder in $folders) {
            $path=Join-Path $ProjectRoot $folder
            if (Test-Path -LiteralPath $path) { Get-ChildItem -LiteralPath $path -Recurse -File }
        })
        $prefix=[IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
        foreach ($file in $files) {
            $relative=$file.FullName.Substring($prefix.Length).Replace('\','/')
            if ($file.Extension -ieq '.bat' -or $file.Name -like 'MANIFIESTO_*.json') { continue }
            if (($relative -notmatch '^ThirdParty/Licenses/' -and $relative -match '(^|/)(bin|obj|dist|cache|Data|\.git|__pycache__)(/|$)') -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { continue }
            if ($allowed -notcontains $file.Extension.ToLowerInvariant() -and $file.Name -ne 'COPYING' -and -not ($relative -like 'ThirdParty/SourcePackages/Libraries/*.zip') -and $relative -ne 'PalClam/Resources/update-public.pem' -and $relative -notmatch '^ThirdParty/Licenses/') { continue }
            if ($file.Name -match '(?i)(private|secret|\.pfx$|\.key$)' -and $relative -notmatch '^PalClam/Infrastructure/AiSecretsStore.cs$') { continue }
            $entrySource=$file.FullName
            if ($relative -eq 'README.md') { $entrySource=Join-Path $ProjectRoot 'BUILD_FROM_SOURCE.md' }
            $sourceRecords+=@([pscustomobject]@{File=$relative;Sha256=(Get-FileHash -LiteralPath $entrySource -Algorithm SHA256).Hash.ToLowerInvariant()})
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$entrySource,$relative,[System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $manifestEntry=$archive.CreateEntry('PUBLIC_SOURCE_MANIFEST.json')
        $manifestStream=$null
        $writer=$null
        try {
            $manifestStream=$manifestEntry.Open()
            $writer=New-Object System.IO.StreamWriter($manifestStream,(New-Object Text.UTF8Encoding($false)))
            $writer.Write(([pscustomobject]@{Version='1.2.2';License='GPL-3.0-or-later';BatWrappersIncluded=$false;Files=$sourceRecords} | ConvertTo-Json -Depth 5))
        } finally {
            try { if ($writer) { $writer.Dispose() } } finally { if ($manifestStream) { $manifestStream.Dispose() } }
        }
    } finally {
        try { if ($archive) { $archive.Dispose() } } finally { if ($stream) { $stream.Dispose() } }
    }
    $check=[System.IO.Compression.ZipFile]::OpenRead($partial)
    try {
        if (@($check.Entries | Where-Object { $_.FullName -match '(?i)\.bat$' }).Count -gt 0) { throw 'Las fuentes publicas no deben contener BAT.' }
        foreach ($required in @('BUILD_FROM_SOURCE.md','Tools/ConstruirDistribucion.ps1','Tools/PublicacionBloqueada.ps1','Installer/PalClam.iss','Directory.Build.props')) {
            if (-not $check.GetEntry($required)) { throw "Fuentes incompletas: $required" }
        }
    } finally { $check.Dispose() }
    Move-Item -LiteralPath $partial -Destination $Destination -Force
    Write-Host '[OK] Fuentes correspondientes preparadas; no se incluyen claves privadas ni datos del usuario.'
} catch {
    Write-Host ('[ERROR FUENTES] '+$_.Exception.ToString()+[Environment]::NewLine+$_.ScriptStackTrace) -ForegroundColor Red
    exit 1
} finally {
    if (Test-Path -LiteralPath $partial) {
        try { Remove-Item -LiteralPath $partial -Force -ErrorAction Stop }
        catch { Write-Host ('[AVISO FUENTES] Parcial conservado para diagnostico: '+$partial+'. '+$_.Exception.Message) }
    }
}
