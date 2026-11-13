# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
param([ValidateSet('win-x64','win-x86')][string]$RuntimeId = $(if ([Environment]::Is64BitOperatingSystem) { 'win-x64' } else { 'win-x86' }))
# PalClam 0.8.0 - genera bases OFFLINE usando FreshClam oficial.
# Se ejecuta al CREAR INSTALADOR en el PC del desarrollador (no en el del usuario).
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent $PSScriptRoot
$engine = Join-Path $root ('ThirdParty\ClamAV\' + $RuntimeId)
$signatures = Join-Path $root 'ThirdParty\OfflineSignatures'
$cache = Join-Path $root 'Tools\cache'
$freshclam = Join-Path $engine 'freshclam.exe'
$sigtool = Join-Path $engine 'sigtool.exe'
if (!(Test-Path -LiteralPath $freshclam)) { throw 'Motor no preparado. Ejecuta PREPARAR_MOTOR.bat.' }
if (!(Test-Path -LiteralPath $sigtool)) { throw 'El paquete de ClamAV no incluye sigtool.exe.' }
New-Item -ItemType Directory -Force -Path $cache, $signatures | Out-Null
$cfg = Join-Path $cache 'freshclam-offline.conf'
$normal = $signatures.Replace('\', '/')
if ($normal.Contains('"')) { throw 'Ruta no compatible con FreshClam.' }
$configLines=@(
  '# PalClam: firmas para distribucion offline',
  ('DatabaseDirectory "' + $normal + '"'),
  'DatabaseMirror database.clamav.net','Checks 12','LogTime yes'
)
[IO.File]::WriteAllText($cfg,($configLines -join "`r`n")+"`r`n",(New-Object Text.UTF8Encoding($false)))

Write-Host '[FIRMAS OFFLINE] Ejecutando FreshClam oficial...'
# Si el desarrollador está sin conexión, podemos usar una caché previamente validada.
& $freshclam ('--config-file=' + $cfg)
$updateExit = $LASTEXITCODE
if ($updateExit -ne 0) { Write-Host "[AVISO] FreshClam no pudo actualizar (codigo $updateExit). Comprobando caché..." -ForegroundColor Yellow }

$found = @{}
foreach ($family in @('main', 'daily', 'bytecode')) {
    $db = @((Join-Path $signatures ($family + '.cld')), (Join-Path $signatures ($family + '.cvd'))) |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (!$db) { throw "No existe la base $family. Sin firmas completas NO se genera instalador offline." }
    if ((Get-Item -LiteralPath $db).Length -lt 1024) { throw "La base $family está vacía o incompleta." }
    Write-Host ("[VERIFICANDO] " + (Split-Path $db -Leaf))
    & $sigtool '--info' $db | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "El archivo $db no superó la validación de SigTool." }
    $found[$family] = $db
}
# Prohibir un instalador de 'protección offline' con firmas demasiado antiguas.
$age = ((Get-Date).ToUniversalTime() - (Get-Item -LiteralPath $found['daily']).LastWriteTimeUtc).TotalDays
if ($age -gt 30 -and $env:PALCLAM_PERMITIR_FIRMAS_ANTIGUAS -ne '1') {
    throw "Las firmas daily tienen $([int]$age) dias. Conéctate para actualizarlas. Para desarrollo, PALCLAM_PERMITIR_FIRMAS_ANTIGUAS=1 permite una caché antigua con advertencia."
}
Write-Host '[OK] Bases main, daily y bytecode comprobadas para el instalador offline.'
if ($age -gt 7) { Write-Host "[AVISO] Firmas con aproximadamente $([int]$age) dias. Actualizarlas al disponer de Internet." -ForegroundColor Yellow }
