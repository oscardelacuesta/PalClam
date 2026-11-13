# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
# PalClam 1.2.2: comprobación explícita de acta, sin instalar ni publicar.
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
    $actaPath = Join-Path $root 'docs\ACEPTACION_1.2.2.ini'
    $setupPath = Join-Path $root 'dist\installer\PalClam_1.2.2_Setup.exe'
    if (-not (Test-Path -LiteralPath $actaPath -PathType Leaf)) { throw 'Falta el acta de 1.2.2.' }
    if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) { throw 'No existe el instalador 1.2.2.' }
    $required = @('PRUEBAS_AUTOMATICAS','COMPILACION_WPF','COMPILACION_SERVICIO',
        'TEMAS_UI_Y_ESCALADO','INSTALACION_LIMPIA','ACTUALIZACION_DESDE_PREVIEW25',
        'DESINSTALACION_Y_DATOS','SERVICIO_INICIO_DETENCION','SERVICIO_USUARIO_ESTANDAR',
        'FIRMAS_ONLINE_OFFLINE_ROLLBACK','GUARD_USB_DESCARGAS',
        'SMART_SCAN_PAUSA_CANCELACION','CUARENTENA_INTEGRIDAD',
        'AI_CONSENTIMIENTO_PRIVACIDAD','REGISTROS_Y_ERRORES','RENDIMIENTO',
        'VERIFICACION_LICENCIAS','APROBACION_AUTOR','GPL3_FUENTES_Y_REFERENCIAS','GPL3_BINARIOS_Y_DISTRIBUIBLES',
        'RECURSOS_SEIS_IDIOMAS','SELECTOR_Y_REINICIO','FALLBACK_Y_FORMATOS',
        'IDIOMA_CON_ANALISIS_ACTIVO','DPI_Y_TEXTOS_LARGOS','COBERTURA_COMPLETA_UI','DETECCION_WINDOWS_Y_PRIORIDAD_MANUAL',
        'ACTUALIZACION_DESDE_120_PREVIEW2','LED_DISCOS_Y_COLORES','LED_PDH_SEIS_IDIOMAS','CORREO_OUTLOOK_CLASICO_SOLO_LECTURA','CORREO_MBOX_MAILDIR_LIMITES','CORREO_GRAPH_CARPETAS','RANSOMWARE_AVISOS_Y_FALSOS_POSITIVOS','RANSOMWARE_EVENTOS_PERDIDOS','RANSOMWARE_PRIVACIDAD_Y_CIERRE','ACTUALIZACION_DESDE_120_PREVIEW3','X86_APLICACION_SERVICIO_MOTOR','X64_APLICACION_SERVICIO_MOTOR','INSTALADOR_AUTOMATICO_ARQUITECTURA','PORTABLE_DATOS_AISLADOS','PORTABLE_SIN_REGISTRO_SERVICIOS','PORTABLE_CAMBIO_EQUIPO_Y_RUTA','EMPEZAR_AQUI_TODAS_SALIDAS')
    $values = @{}
    foreach ($line in (Get-Content -LiteralPath $actaPath -Encoding UTF8)) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^([A-Z_0-9]+)=(\S+)\s*$') { throw 'Acta con sintaxis no válida.' }
        $key = $Matches[1]; $value = $Matches[2]
        if ($values.ContainsKey($key)) { throw "Clave duplicada: $key" }
        $values[$key] = $value
    }
    if (-not $values.ContainsKey('VERSION') -or $values['VERSION'] -cne '1.2.2') {
        throw 'El acta no corresponde a 1.2.2.'
    }
    foreach ($key in @($values.Keys)) {
        if ($key -ne 'VERSION' -and $key -notin $required) { throw "Clave desconocida: $key" }
    }
    $pending = @($required | Where-Object { -not $values.ContainsKey($_) -or $values[$_] -cne 'CONFORME' })
    if ($pending.Count -gt 0) { throw ('Pendientes de validación real: ' + ($pending -join ', ')) }
    $exe = Get-Item -LiteralPath $setupPath
    if ($exe.Length -lt 1000000) { throw 'El instalador no alcanza el tamaño mínimo de comprobación.' }
    $sha = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
    $reportPath = Join-Path $root 'dist\installer\ACEPTACION_1.2.2_FINAL_VALIDADA.txt'
    @('PalClam 1.2.2: acta manual completa',
      ('Fecha UTC: ' + (Get-Date).ToUniversalTime().ToString('o')),
      ('SHA256 instalador: ' + $sha),
      'Esta comprobación NO ejecuta ensayos, no firma software y NO publica actualizaciones.',
      'Conservar informes/capturas/logs de cada comprobación.') |
      Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host '[OK] Acta completa; comprobación documental superada. Conservar evidencias. NO se ha publicado.' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ('[BLOQUEADO] ' + $_.Exception.Message) -ForegroundColor Yellow
    exit 1
}
