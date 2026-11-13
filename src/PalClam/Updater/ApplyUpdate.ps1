# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
# PalClam updater: ejecutado FUERA de PalClam, tras verificar un manifiesto RSA y su SHA256.
param(
    [Parameter(Mandatory=$true)][string] $InstallerPath,
    [Parameter(Mandatory=$true)][string] $ExpectedSha256,
    [Parameter(Mandatory=$true)][int] $ParentPid,
    [Parameter(Mandatory=$true)][string] $AppExe
)
$ErrorActionPreference = 'Stop'
$folder = Split-Path -Parent $InstallerPath
$log = Join-Path $folder 'actualizacion.log'
function Log([string] $message) {
    Add-Content -LiteralPath $log -Encoding UTF8 -Value ("[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'),$message)
}
try {
    Log 'Actualizacion solicitada. Esperando al cierre de PalClam.'
    try { Wait-Process -Id $ParentPid -Timeout 180 -ErrorAction Stop } catch { }
    if (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue) {
        throw 'PalClam no se ha cerrado. Se cancela la instalacion.'
    }
    if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
        throw 'El instalador descargado ha desaparecido.'
    }
    $actual = (Get-FileHash -LiteralPath $InstallerPath -Algorithm SHA256).Hash
    if ($actual -ine $ExpectedSha256) { throw 'El instalador ha cambiado. SHA256 incorrecto.' }
    Log 'Hash validado. Iniciando Inno Setup en modo silencioso.'
    $arguments = '/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /NORESTARTAPPLICATIONS'
    $result = Start-Process -FilePath $InstallerPath -ArgumentList $arguments -PassThru -Wait
    Log "Inno Setup finalizo con codigo $($result.ExitCode)."
    if ($result.ExitCode -ne 0) { throw "Error de instalacion: $($result.ExitCode)" }
    if (Test-Path -LiteralPath $AppExe -PathType Leaf) {
        Start-Process -FilePath $AppExe | Out-Null
        Log 'PalClam reiniciado.'
    } else {
        Log 'Instalacion finalizada pero no se localizo la ruta original del ejecutable.'
    }
} catch {
    Log ('ERROR: ' + $_.Exception.Message)
    # El usuario puede consultar actualizacion.log en LOCALAPPDATA\PalClam\Updates\version\
    exit 1
}
