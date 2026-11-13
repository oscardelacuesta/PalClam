# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
# PalClam RC1: comprobación explícita de acta, sin instalar ni publicar.
param([string]$ProjectRoot)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
try {
    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $actaPath = Join-Path $root 'docs\ACEPTACION_1.1.0_RC1.ini'
    $setupPath = Join-Path $root 'dist\installer\PalClam_1.1.0_RC1_Setup_win-x64.exe'
    if (-not (Test-Path -LiteralPath $actaPath -PathType Leaf)) { throw 'Falta el acta de RC1.' }
    if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) { throw 'No existe instalador RC1 validado.' }
    $required = @('PRUEBAS_AUTOMATICAS','COMPILACION_WPF','COMPILACION_SERVICIO',
        'TEMAS_UI_Y_ESCALADO','INSTALACION_LIMPIA','ACTUALIZACION_DESDE_PREVIEW25',
        'DESINSTALACION_Y_DATOS','SERVICIO_INICIO_DETENCION','SERVICIO_USUARIO_ESTANDAR',
        'FIRMAS_ONLINE_OFFLINE_ROLLBACK','GUARD_USB_DESCARGAS',
        'SMART_SCAN_PAUSA_CANCELACION','CUARENTENA_INTEGRIDAD',
        'AI_CONSENTIMIENTO_PRIVACIDAD','REGISTROS_Y_ERRORES','RENDIMIENTO',
        'VERIFICACION_LICENCIAS','APROBACION_AUTOR')
    $values = @{}
    foreach ($line in (Get-Content -LiteralPath $actaPath -Encoding UTF8)) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^([A-Z_0-9]+)=(\S+)\s*$') { throw 'Acta con sintaxis no válida.' }
        $key = $Matches[1]; $value = $Matches[2]
        if ($values.ContainsKey($key)) { throw "Clave duplicada: $key" }
        $values[$key] = $value
    }
    if (-not $values.ContainsKey('VERSION') -or $values['VERSION'] -cne '1.1.0-rc.1') {
        throw 'El acta no corresponde a RC1.'
    }
    foreach ($key in @($values.Keys)) {
        if ($key -ne 'VERSION' -and $key -notin $required) { throw "Clave desconocida: $key" }
    }
    $pending = @($required | Where-Object { -not $values.ContainsKey($_) -or $values[$_] -cne 'CONFORME' })
    if ($pending.Count -gt 0) { throw ('Pendientes de validación real: ' + ($pending -join ', ')) }
    $exe = Get-Item -LiteralPath $setupPath
    if ($exe.Length -lt 1000000) { throw 'El instalador no alcanza el tamaño mínimo de comprobación.' }
    $sha = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
    $reportPath = Join-Path $root 'dist\installer\ACEPTACION_1.1.0_RC1_VALIDADA.txt'
    @('PalClam RC1: acta manual completa',
      ('Fecha UTC: ' + (Get-Date).ToUniversalTime().ToString('o')),
      ('SHA256 instalador: ' + $sha),
      'Esta comprobación NO ejecuta ensayos, no firma software y NO publica actualizaciones.',
      'Conservar informes/capturas/logs de cada comprobación.') |
      Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host '[OK] Acta completa; comprobación documental superada. NO se ha publicado.' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ('[BLOQUEADO] ' + $_.Exception.Message) -ForegroundColor Yellow
    exit 1
}
