# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
# Puerta de publicación LOCAL: nunca publica ni sube archivos.
param([string]$ProjectRoot)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
try {
    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $file = Join-Path $root 'docs\ACEPTACION_1.0.0.ini'
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Falta acta: $file" }
    $data = @{}
    foreach ($line in Get-Content -LiteralPath $file) {
        if ($line -match '^\s*(?:#|$)') { continue }
        if ($line -notmatch '^([A-Z_0-9]+)=(.+)$') { throw "Linea de acta no reconocida: $line" }
        $key = $Matches[1]; $value = $Matches[2].Trim()
        if ($data.ContainsKey($key)) { throw "Clave duplicada en acta: $key" }
        $data[$key] = $value
    }
    if (-not $data.ContainsKey('VERSION') -or $data['VERSION'] -ne '1.0.0') {
        throw 'El acta no corresponde a PalClam 1.0.0'
    }
    $required = @('COMPILACION','INSTALADOR','INSTALACION_LIMPIA','ACTUALIZACION_DESDE_RC8',
        'CLAMAV_OFFLINE','FIRMAS','ESCANEOS_Y_CANCELACION','CUARENTENA_EICAR','RENDIMIENTO',
        'GUARD','MAIL_Y_OUTLOOK','PROGRAMACION','ACTUALIZADOR_FIRMADO',
        'DESINSTALACION','REVISION_LEGAL','APROBACION_AUTOR')
    $pending = @($required | Where-Object { -not $data.ContainsKey($_) -or $data[$_] -ne 'CONFORME' })
    if ($pending.Count -gt 0) { throw ('Aprobacion pendiente: ' + ($pending -join ', ')) }
    Write-Host '[OK] Acta de PalClam 1.0.0 completada. Solo se autoriza la preparacion local del manifiesto.' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ('[BLOQUEADO] ' + $_.Exception.Message) -ForegroundColor Yellow
    exit 1
}
