# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
# Validador local del paquete PalClam 1.1.0. No instala, no borra archivos del usuario
# y no publica nada en Internet. Requiere preparar el motor y firmas con los BAT.
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

function Fail([string]$message) { throw "[VALIDACION FALLIDA] $message" }
function NeedFile([string]$path, [int64]$minBytes = 1) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Falta $path" }
    $info = Get-Item -LiteralPath $path
    if ($info.Length -lt $minBytes) { Fail "Archivo vacío, incompleto o demasiado pequeño: $path" }
    return $info
}

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$project = Join-Path $root 'PalClam\PalClam.csproj'
$iss = Join-Path $root 'Installer\PalClam.iss'
$exe = Join-Path $root 'PalClam\dist\win-x64\PalClam.exe'
$setup = Join-Path $root 'dist\installer\PalClam_1.2.0_Setup_win-x64.exe'
$engine = Join-Path $root 'ThirdParty\ClamAV'
$db = Join-Path $root 'ThirdParty\OfflineSignatures'
$source = Join-Path $root 'ThirdParty\SourcePackages\clamav-1.5.4.tar.gz'
$license = Join-Path $root 'LICENSE-GPL-3.0.txt'

try {
    Write-Host '[1/5] Version, instalador y fuentes...' -ForegroundColor Cyan
    [xml]$xml = Get-Content -LiteralPath $project -Raw
    $version = [string]$xml.Project.PropertyGroup.Version
    if ($version -ne '1.2.0') { Fail "Version del proyecto incorrecta: $version" }
    $text = Get-Content -LiteralPath $iss -Raw
    if ($text -notmatch '(?m)^AppVerName=\{#MyAppName\} 1\.2\.0\s*$' -or $text -notmatch '(?m)^OutputBaseFilename=PalClam_1\.2\.0_Setup_win-x64\s*$') {
        Fail 'Inno Setup no apunta a 1.2.0.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'Tools\VerificarFuentes.ps1'))) {
        Fail 'No existe la comprobacion previa de fuentes.'
    }
    [void](NeedFile $exe 100000)
    [void](NeedFile (Join-Path $root 'PalClam.Service\dist\win-x64\PalClam.Service.exe') 100000)
    [void](NeedFile (Join-Path $root 'Tools\MachineService.ps1') 100)
    $setupInfo = NeedFile $setup 1000000
    [void](NeedFile $source 10000)
    [void](NeedFile $license 1000)
    [void](NeedFile (Join-Path $root 'THIRD-PARTY-NOTICES.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.0.0_RC5.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.0.0_RC8.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.0.0.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW5.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW6.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW8.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW9.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW10.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW11.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW17.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW19.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW20.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_RC1.md') 100)
    [void](NeedFile (Join-Path $root 'CAMBIOS_1.1.0_FINAL.md') 100)

    Write-Host '[2/5] Motor ClamAV offline...' -ForegroundColor Cyan
    $scanner = Join-Path $engine 'clamscan.exe'
    $freshclam = Join-Path $engine 'freshclam.exe'
    $sigtool = Join-Path $engine 'sigtool.exe'
    foreach ($binary in @($scanner, $freshclam, $sigtool)) {
        [void](NeedFile $binary 10000)
    }
    & $scanner '--version' | Out-Host
    if ($LASTEXITCODE -ne 0) { Fail "clamscan --version dio $LASTEXITCODE" }
    # FreshClam en Windows intenta leer freshclam.conf incluso con --version.
    # El ZIP oficial no incorpora necesariamente una configuración activa.
    # Crear una configuración temporal como la usada al preparar firmas y en
    # PalClam. No ejecutar una actualización ni escribir en las firmas.
    $configTemp = Join-Path ([System.IO.Path]::GetTempPath()) ('palclam-freshclam-validacion-' + [guid]::NewGuid().ToString('N') + '.conf')
    try {
        $dbConfig = $db.Replace('\', '/')
        if ($dbConfig.Contains('"') -or $dbConfig.Contains("`n") -or $dbConfig.Contains("`r")) {
            Fail 'La ruta de firmas contiene caracteres no admitidos por FreshClam.'
        }
        $configContent = @(
            '# PalClam: configuracion temporal de validacion; no actualiza firmas',
            ('DatabaseDirectory "' + $dbConfig + '"'),
            'DatabaseMirror database.clamav.net',
            'Checks 12',
            'LogTime yes'
        ) -join "`r`n"
        [System.IO.File]::WriteAllText($configTemp, $configContent + "`r`n", (New-Object System.Text.UTF8Encoding($false)))
        & $freshclam ('--config-file=' + $configTemp) '--version' | Out-Host
        if ($LASTEXITCODE -ne 0) { Fail "freshclam --version con configuración temporal dio $LASTEXITCODE" }
        Write-Host '[OK] FreshClam ejecutado con configuracion temporal valida, sin actualizar firmas.' -ForegroundColor Green
    }
    finally {
        if (Test-Path -LiteralPath $configTemp) { Remove-Item -LiteralPath $configTemp -Force }
    }

    Write-Host '[3/5] Bases de firmas de respaldo...' -ForegroundColor Cyan
    foreach ($family in @('main', 'daily', 'bytecode')) {
        $base = @((Join-Path $db "$family.cvd"), (Join-Path $db "$family.cld")) |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
        if ($null -eq $base) { Fail "No existe base de firmas $family" }
        [void](NeedFile $base 128)
        & $sigtool "--info=$base" | Out-Null
        if ($LASTEXITCODE -ne 0) { Fail "sigtool no reconoce base $base (codigo $LASTEXITCODE)" }
    }

    Write-Host '[4/5] Prueba inofensiva de ClamAV con firmas offline...' -ForegroundColor Cyan
    $temp = Join-Path $env:TEMP ('PalClam-Validacion-' + [guid]::NewGuid().ToString('N') + '.txt')
    try {
        Set-Content -LiteralPath $temp -Encoding Ascii -Value 'PalClam 1.1.0 preview 8: archivo de prueba inocuo, sin firmas especiales.'
        & $scanner "--database=$db" '--no-summary' '--stdout' $temp | Out-Host
        if ($LASTEXITCODE -ne 0) { Fail "El motor offline no terminó correctamente (código $LASTEXITCODE)" }
    }
    finally { if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force } }

    Write-Host '[5/5] SHA-256 del instalador...' -ForegroundColor Cyan
    $sha256 = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
    $report = @(
        'PalClam 1.2.0 - validacion local de paquete',
        ('Fecha UTC: ' + (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('Proyecto: ' + $version),
        ('Instalador: ' + $setupInfo.FullName),
        ('Tamano: ' + $setupInfo.Length + ' bytes'),
        ('SHA-256: ' + $sha256),
        'Motor: clamscan y freshclam ejecutados',
        'Firmas: main, daily y bytecode verificadas con sigtool',
        'Prueba de archivo inofensivo: completada sin detecciones',
        'NO VALIDA: interfaz, Outlook, cuarentena, Guard, instalacion en equipo limpio ni licencias.',
        'La preparacion local no equivale a publicacion; requiere acta de aceptacion.'
    )
    $reportPath = Join-Path $root 'dist\installer\VALIDACION_1.2.0.txt'
    $report | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host "[OK] Paquete offline comprobado. Informe: $reportPath" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
