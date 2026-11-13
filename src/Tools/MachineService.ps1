# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([ValidateSet('Install','Remove','Status','RefreshSignatures','UpdateOnlineSignatures','Start','Stop','Restart')][string]$Action = 'Status')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$serviceName = 'PalClamGuardSvc'
$root = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$exe = Join-Path $root 'PalClam.Service.exe'
$watch = Join-Path $env:ProgramData 'PalClam\GuardWatch'
$state = Join-Path $env:ProgramData 'PalClam\MachineGuard'
$sharedDb = Join-Path $state 'Signatures'
$sourceDb = Join-Path $root 'OfflineSignatures'
$scannerTools = Join-Path $root 'ClamAV\sigtool.exe'
$freshclam = Join-Path $root 'ClamAV\freshclam.exe'

function Require-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Se necesita autorización de administrador (UAC).'
    }
}
function Is-SharedInstall {
    foreach ($folder in @([Environment]::GetFolderPath('ProgramFiles'), [Environment]::GetFolderPath('ProgramFilesX86'))) {
        if ($folder -and $root.StartsWith(([IO.Path]::GetFullPath($folder).TrimEnd('\') + '\'),
            [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}
function Get-PalClamService {
    return Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction Stop
}
function Check-Ownership($service) {
    if ($null -eq $service) { return }
    if (([string]$service.PathName).Trim().Trim('"') -ine $exe) {
        throw "Existe otro servicio PalClam en una ruta distinta. No se modifica: $($service.PathName)"
    }
}
function Run-Sc([string[]]$arguments) {
    & "$env:SystemRoot\System32\sc.exe" @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "sc.exe devolvió $LASTEXITCODE : $($arguments -join ' ')" }
}
function Run-Icacls([string]$path, [string[]]$arguments) {
    & "$env:SystemRoot\System32\icacls.exe" $path @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "No se pudieron establecer permisos seguros de $path" }
}
function Set-SecureStateAcl {
    # El servicio puede escribir diagnósticos, pero Usuarios solo leerlos.
    # Impedir heredar permisos de escritura de una instalación anterior.
    Run-Icacls $state @('/inheritance:r', '/grant:r',
      '*S-1-5-18:(OI)(CI)F', '*S-1-5-32-544:(OI)(CI)F',
      "NT SERVICE\${serviceName}:(OI)(CI)M", '*S-1-5-32-545:(OI)(CI)RX')
}
function Set-SecureSignatureAcl([string]$folder) {
    # Las bases son administradas solo por administradores; el servicio solo lee.
    Run-Icacls $folder @('/inheritance:r', '/grant:r',
      '*S-1-5-18:(OI)(CI)F', '*S-1-5-32-544:(OI)(CI)F',
      "NT SERVICE\${serviceName}:(OI)(CI)RX", '*S-1-5-32-545:(OI)(CI)RX')
}
function Wait-ForStopped {
    for ($i=0; $i -lt 60; $i++) {
        $value = Get-PalClamService
        if ($null -eq $value -or $value.State -eq 'Stopped') { return }
        Start-Sleep -Milliseconds 500
    }
    throw 'El servicio no se ha detenido en el tiempo previsto. No se cambian las firmas.'
}
function Wait-ForRunning {
    # sc.exe start puede devolver 0 y el proceso fallar justo después.
    # Dos consultas consecutivas al SCM reducen falsos éxitos de instalación.
    for ($i=0; $i -lt 40; $i++) {
        $service = Get-PalClamService
        if ($null -eq $service) { throw 'El servicio ya no está registrado.' }
        if ($service.State -eq 'Running') {
            Start-Sleep -Milliseconds 500
            $confirmed = Get-PalClamService
            if ($null -ne $confirmed -and $confirmed.State -eq 'Running') { return }
        }
        Start-Sleep -Milliseconds 500
    }
    throw 'El servicio no alcanza Running de forma estable. Revisar estado.json y eventos.log; no se confirma protección.'
}
# Una fecha de modificacion NO acredita la version real. Comparar Version:
# informado por sigtool, y rechazar una sustitucion si no es verificable.
function Get-SignatureVersion([string]$file) {
    $info = @(& $scannerTools ("--info=$file") 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "sigtool rechazo una base: $file" }
    foreach ($line in $info) {
        if ([string]$line -match '^\s*Version:\s*(\d+)\s*$') {
            return [long]$Matches[1]
        }
    }
    throw "sigtool no comunico Version numerica de $file; no se cambiaron las firmas."
}

function Prepare-OnlineStage([string]$stage) {
    # Todo queda bajo ProgramData con ACL de escritura administrativa.
    New-Item -ItemType Directory -Force -Path $state | Out-Null
    Set-SecureStateAcl
    New-Item -ItemType Directory -Path $stage -ErrorAction Stop | Out-Null
    Set-SecureSignatureAcl $stage
    foreach ($family in @('main','daily','bytecode')) {
        $sourceFile = @((Join-Path $sharedDb "$family.cld"),
                        (Join-Path $sharedDb "$family.cvd"),
                        (Join-Path $sourceDb "$family.cld"),
                        (Join-Path $sourceDb "$family.cvd")) |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
        if ($null -ne $sourceFile) {
            [void](Get-SignatureVersion ([string]$sourceFile))
            Copy-Item -LiteralPath ([string]$sourceFile) -Destination $stage -ErrorAction Stop
        }
    }
}

function Update-SharedSignaturesOnline {
    if (-not (Test-Path -LiteralPath $freshclam -PathType Leaf)) {
        throw 'No existe freshclam.exe oficial en el motor instalado.'
    }
    if (-not (Test-Path -LiteralPath $scannerTools -PathType Leaf)) {
        throw 'No existe sigtool.exe para comprobar las bases descargadas.'
    }
    $onlineStage = Join-Path $state ('Signatures-online-' + [guid]::NewGuid().ToString('N'))
    $configPath = Join-Path $state ('freshclam-online-' + [guid]::NewGuid().ToString('N') + '.conf')
    $wasRunning = $false
    $stopped = $false
    try {
        Prepare-OnlineStage $onlineStage
        $dbDir = $onlineStage.Replace('\', '/')
        if ($dbDir.Contains('"') -or $dbDir.Contains("`r") -or $dbDir.Contains("`n")) {
            throw 'Ruta temporal de firmas no valida.'
        }
        $lines = @('# PalClam Machine Guard - FreshClam oficial',
            ('DatabaseDirectory "' + $dbDir + '"'),
            'DatabaseMirror database.clamav.net',
            'Checks 12', 'LogTime yes')
        [IO.File]::WriteAllText($configPath, (($lines -join "`r`n") + "`r`n"),
            (New-Object Text.UTF8Encoding($false)))
        & $freshclam ("--config-file=$configPath") | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "FreshClam devolvio codigo $LASTEXITCODE. Las firmas compartidas no han cambiado."
        }
        # Validacion previa, con el servicio todavia activo en su copia antigua.
        foreach ($family in @('main','daily')) {
            $file = @((Join-Path $onlineStage "$family.cld"),
                (Join-Path $onlineStage "$family.cvd")) |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
            if ($null -eq $file) { throw "No existe la base $family despues de FreshClam." }
            [void](Get-SignatureVersion ([string]$file))
        }
        $service = Get-PalClamService
        Check-Ownership $service
        if ($null -eq $service) { throw 'Machine Guard no esta instalado.' }
        $wasRunning = $service.State -eq 'Running'
        if ($wasRunning) {
            Run-Sc @('stop',$serviceName)
            Wait-ForStopped
            $stopped = $true
        }
        Refresh-SharedSignatures -sourceDirectory $onlineStage
        # Confirmar el resultado solo DESPUÉS de comprobar el reinicio del servicio.
        $updated = $true
    }
    finally {
        # Tras una renovación, comprobar también que SCM sostiene el servicio.
        # Un fallo no debe ocultarse bajo el mensaje "firmas actualizadas".
        $restartError = $null
        if ($stopped) {
            try { Run-Sc @('start',$serviceName); Wait-ForRunning }
            catch { $restartError = $_.Exception.Message }
        }
        if (Test-Path -LiteralPath $configPath -PathType Leaf) {
            Remove-Item -LiteralPath $configPath -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $onlineStage -PathType Container) {
            Remove-Item -LiteralPath $onlineStage -Recurse -Force -ErrorAction SilentlyContinue
        }
        if ($null -ne $restartError) {
            throw ('La actualización terminó, pero el servicio no se recuperó: ' + $restartError)
        }
    }
    if ($updated) {
        Write-Host '[OK] FreshClam online: bases compartidas verificadas, servicio recuperado si estaba activo.'
    }
}

function Refresh-SharedSignatures([string]$sourceDirectory = $sourceDb) {
    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw 'No existen firmas de respaldo en la instalación. Ejecuta PREPARAR_OFFLINE en la construcción.'
    }
    if (-not (Test-Path -LiteralPath $scannerTools -PathType Leaf)) {
        throw 'No existe sigtool.exe para comprobar la estructura de las bases.'
    }
    New-Item -ItemType Directory -Force -Path $state | Out-Null
    Set-SecureStateAcl
    $stage = Join-Path $state ('Signatures-stage-' + [guid]::NewGuid().ToString('N'))
    $backup = Join-Path $state ('Signatures-backup-' + [guid]::NewGuid().ToString('N'))
    $swapped = $false
    $completed = $false
    try {
        New-Item -ItemType Directory -Path $stage | Out-Null
        Set-SecureSignatureAcl $stage
        foreach ($family in @('main','daily','bytecode')) {
            $files = @((Join-Path $sourceDirectory "$family.cvd"), (Join-Path $sourceDirectory "$family.cld")) |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
            if ($files.Count -eq 0) {
                if ($family -eq 'bytecode') { continue }
                throw "Falta la base oficial $family en $sourceDirectory"
            }
            # Si hay CVD y CLD, preferir CLD (más nueva normalmente), nunca mezclar ambas.
            $chosen = @($files | Where-Object { $_ -like '*.cld' } | Select-Object -First 1)
            if ($chosen.Count -eq 0) { $chosen = @($files | Select-Object -First 1) }
            $src = [string]$chosen[0]
            if ((Get-Item -LiteralPath $src).Length -lt 128) { throw "Base incompleta: $src" }
            # Comparar la version interna, no la fecha del archivo.
            $candidateVersion = Get-SignatureVersion $src
            $current = @((Join-Path $sharedDb "$family.cvd"), (Join-Path $sharedDb "$family.cld")) |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
            foreach ($old in $current) {
                $currentVersion = Get-SignatureVersion ([string]$old)
                if ($currentVersion -gt $candidateVersion) {
                    throw "La base compartida $family tiene version $currentVersion y la candidata $candidateVersion. Se evita retroceder."
                }
            }
            $dest = Join-Path $stage ([IO.Path]::GetFileName($src))
            Copy-Item -LiteralPath $src -Destination $dest -ErrorAction Stop
            $a = (Get-FileHash -LiteralPath $src -Algorithm SHA256).Hash
            $b = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash
            if ($a -ne $b) { throw "SHA-256 distinto tras copiar $src" }
        }
        Set-SecureSignatureAcl $stage
        # Probar que el motor realmente puede cargar las firmas ANTES de
        # intercambiar los directorios. sigtool --info por sí solo no basta.
        $scanner = Join-Path $root 'ClamAV\clamscan.exe'
        if (-not (Test-Path -LiteralPath $scanner -PathType Leaf)) {
            throw 'Falta clamscan.exe para validar las firmas candidatas.'
        }
        $sample = Join-Path $state ('palclam-verify-' + [guid]::NewGuid().ToString('N') + '.txt')
        try {
            [IO.File]::WriteAllText($sample, 'PalClam signature validation: harmless file.')
            & $scanner ("--database=$stage") '--no-summary' '--stdout' $sample | Out-Host
            if ($LASTEXITCODE -ne 0) {
                throw "ClamAV no cargo correctamente las firmas candidatas (codigo $LASTEXITCODE)."
            }
        }
        finally {
            if (Test-Path -LiteralPath $sample -PathType Leaf) {
                Remove-Item -LiteralPath $sample -Force -ErrorAction SilentlyContinue
            }
        }
        if (Test-Path -LiteralPath $sharedDb -PathType Container) {
            Move-Item -LiteralPath $sharedDb -Destination $backup -ErrorAction Stop
        }
        try {
            Move-Item -LiteralPath $stage -Destination $sharedDb -ErrorAction Stop
            $swapped = $true
        }
        catch {
            if (Test-Path -LiteralPath $backup -PathType Container) {
                Move-Item -LiteralPath $backup -Destination $sharedDb -ErrorAction Stop
            }
            throw
        }
        Set-SecureSignatureAcl $sharedDb
        $completed = $true
        Write-Host '[OK] Firmas compartidas copiadas, validadas con sigtool y SHA-256.'
    }
    finally {
        if ($swapped -and -not $completed) {
            if (Test-Path -LiteralPath $sharedDb) { Remove-Item -LiteralPath $sharedDb -Recurse -Force }
            if (Test-Path -LiteralPath $backup) {
                Move-Item -LiteralPath $backup -Destination $sharedDb -ErrorAction Stop
            }
        }
        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
        if ($completed -and (Test-Path -LiteralPath $backup)) {
            Remove-Item -LiteralPath $backup -Recurse -Force
        }
    }
}

try {
    if ($Action -eq 'Status') {
        $service = Get-PalClamService
        if ($null -eq $service) { Write-Host 'Servicio no instalado.'; exit 0 }
        Check-Ownership $service
        Write-Host ("Servicio: {0}, estado: {1}, inicio: {2}" -f $service.Name, $service.State, $service.StartMode)
        Write-Host ('Firmas compartidas: ' + $(if (Test-Path -LiteralPath $sharedDb) { 'presentes' } else { 'ausentes' }))
        exit 0
    }
    Require-Admin
    if (-not (Is-SharedInstall)) { throw 'Instala PalClam para todos los usuarios en Program Files antes de continuar.' }
    # Proteger la sustitucion frente a otra sesion de Windows que use la misma herramienta.
    if ($Action -in @('Install','RefreshSignatures','UpdateOnlineSignatures')) {
        $guardMutex = [System.Threading.Mutex]::new($false, 'Global\PalClam.SignatureMaintenance')
        try { $guardAcquired = $guardMutex.WaitOne(0) }
        catch [System.Threading.AbandonedMutexException] {
            throw 'Actualizacion interrumpida anteriormente; revisa las firmas antes de reintentar.'
        }
        if (-not $guardAcquired) { throw 'Ya existe una actualizacion de firmas en curso para el equipo.' }
    }
    $current = Get-PalClamService
    Check-Ownership $current

    # Acciones explícitas de administración. Solo el script elevado cambia
    # el estado del servicio y únicamente si apunta a nuestro propio EXE.
    if ($Action -in @('Start','Stop','Restart')) {
        if ($null -eq $current) { throw 'El servicio no está instalado. Usa Instalar y activar.' }
        if ($Action -in @('Stop','Restart')) {
            if ($current.State -eq 'StopPending') { Wait-ForStopped }
            elseif ($current.State -ne 'Stopped') {
                Run-Sc @('stop',$serviceName)
                Wait-ForStopped
            }
        }
        if ($Action -in @('Start','Restart')) {
            $latest = Get-PalClamService
            if ($latest.State -eq 'StopPending') { Wait-ForStopped; $latest = Get-PalClamService }
            if ($latest.State -ne 'Running') { Run-Sc @('start',$serviceName) }
            Wait-ForRunning
        }
        Write-Host ("[OK] Acción solicitada: $Action. No se modificaron firmas ni carpetas.")
        exit 0
    }

    if ($Action -eq 'UpdateOnlineSignatures') {
        if ($null -eq $current) { throw 'Primero instala Machine Guard.' }
        Update-SharedSignaturesOnline
        exit 0
    }

    if ($Action -eq 'Remove') {
        if ($null -eq $current) { Write-Host 'Servicio no instalado.'; exit 0 }
        if ($current.State -ne 'Stopped') { Run-Sc @('stop',$serviceName); Wait-ForStopped }
        Run-Sc @('delete',$serviceName)
        Write-Host '[OK] Servicio retirado. Se conservan archivos y registros del usuario.'
        exit 0
    }

    if ($Action -eq 'RefreshSignatures' -and $null -eq $current) {
        throw 'Primero instala el servicio desde PalClam.'
    }
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Falta binario del servicio: $exe" }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'ClamAV\clamscan.exe') -PathType Leaf)) {
        throw 'Falta el motor ClamAV compartido.'
    }
    if ($null -eq $current) {
        $created = Invoke-CimMethod -ClassName Win32_Service -MethodName Create -Arguments @{
            Name=$serviceName; DisplayName='PalClam Machine Guard (experimental)';
            PathName=('"' + $exe + '"'); ServiceType=[uint32]16;
            StartMode='Automatic'; StartName='NT AUTHORITY\LocalService'; DesktopInteract=$false
        }
        if ($null -eq $created -or $created.ReturnValue -ne 0) {
            throw "No se pudo registrar el servicio (código CIM $($created.ReturnValue))."
        }
    }
    Run-Sc @('sidtype',$serviceName,'unrestricted')
    Run-Sc @('config',$serviceName,'start=','delayed-auto')
    # Reiniciar tras fallos reales de proceso; no oculta errores de firmas o saturación.
    Run-Sc @('failure',$serviceName,'reset=','86400','actions=','restart/60000/restart/60000')
    New-Item -ItemType Directory -Force -Path $watch,$state | Out-Null
    Run-Icacls $watch @('/grant:r','*S-1-5-32-545:(OI)(CI)M')
    Set-SecureStateAcl
    $wasRunning = (Get-PalClamService).State -eq 'Running'
    if ($wasRunning) { Run-Sc @('stop',$serviceName); Wait-ForStopped }
    $restartError = $null
    try { Refresh-SharedSignatures }
    finally {
        if ($wasRunning) {
            try { Run-Sc @('start',$serviceName); Wait-ForRunning }
            catch { $restartError = $_.Exception.Message }
        }
    }
    if ($null -ne $restartError) { throw ('Firmas renovadas, pero Machine Guard no reinició: ' + $restartError) }
    if ($Action -eq 'Install' -and -not $wasRunning) {
        Run-Sc @('start',$serviceName)
        Wait-ForRunning
    }
    # RefreshSignatures sobre servicio detenido conserva la decisión del usuario.
    Write-Host '[OK] Machine Guard configurado. No hay cuarentena ni bloqueo preventivo.'
    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
