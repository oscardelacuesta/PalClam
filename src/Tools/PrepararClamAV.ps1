# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([ValidateSet('win-x64','win-x86')][string]$RuntimeId='win-x64')
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
Import-Module (Join-Path $PSScriptRoot 'Distribution.psm1') -Force -DisableNameChecking
$repo=Split-Path -Parent $PSScriptRoot
try {
    $metadata=Get-EngineMetadata $RuntimeId
    $cache=Join-Path $repo 'Tools\cache'
    $zip=Join-Path $cache $metadata.Asset
    $destination=Join-Path $repo ('ThirdParty\ClamAV\'+$RuntimeId)
    Write-Host "[MOTOR $RuntimeId] Descarga o cache con SHA256 fijado..."
    Get-VerifiedFile $metadata.Url $zip $metadata.Sha256
    $source=Join-Path $repo 'ThirdParty\SourcePackages\clamav-1.5.4.tar.gz'
    Get-VerifiedFile 'https://www.clamav.net/downloads/production/clamav-1.5.4.tar.gz' $source '1af1117a228f1b5bc7fa91a0dabc37848a99e7d25188e9be8043332ce721dfd3'
    $temporary=Join-Path $cache ('engine-expanded-'+$RuntimeId)
    Remove-BuildFolder $repo ('Tools\cache\engine-expanded-'+$RuntimeId)
    New-Item -ItemType Directory -Force -Path $temporary | Out-Null
    Expand-Archive -LiteralPath $zip -DestinationPath $temporary -Force
    $nested=Join-Path $temporary $metadata.Root
    $engineRoot=if (Test-Path -LiteralPath (Join-Path $nested 'clamscan.exe')) { $nested } else { $temporary }
    foreach ($binary in @('clamscan.exe','freshclam.exe','sigtool.exe')) { Assert-PeArchitecture (Join-Path $engineRoot $binary) $RuntimeId }
    Remove-BuildFolder $repo ('ThirdParty\ClamAV\'+$RuntimeId)
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Get-ChildItem -LiteralPath $engineRoot -Force | Copy-Item -Destination $destination -Recurse -Force
    Set-Content -LiteralPath (Join-Path $destination 'PalClam-Engine.txt') -Value $metadata.Version -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $destination 'PalClam-Architecture.txt') -Value $RuntimeId -Encoding Ascii
    Remove-BuildFolder $repo ('Tools\cache\engine-expanded-'+$RuntimeId)
    Write-Host "[OK] Motor oficial $RuntimeId preparado y arquitectura PE comprobada."
    exit 0
} catch { Write-Host ('[ERROR] '+$_.Exception.Message) -ForegroundColor Red; exit 1 }
