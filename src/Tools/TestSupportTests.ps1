# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot)
$toolsDirectory=[IO.Path]::GetDirectoryName($PSCommandPath)
if([string]::IsNullOrWhiteSpace($ProjectRoot)){$ProjectRoot=[IO.Path]::GetFullPath((Join-Path $toolsDirectory '..'))}
$ErrorActionPreference='Stop'
Import-Module (Join-Path $toolsDirectory 'TestSupport.psm1') -Force -DisableNameChecking
$ps=Join-Path $PSHOME 'powershell.exe'
if(-not(Test-Path -LiteralPath $ps)){$ps=Join-Path $PSHOME 'pwsh';if(-not(Test-Path -LiteralPath $ps)){$ps=Join-Path $PSHOME 'pwsh.exe'}}
$count=0
function Check([bool]$Value,[string]$Label){if(-not $Value){throw $Label};$script:count++}
$workspace=New-PalClamTestWorkspace -Kind Support
$extra=New-Object Collections.Generic.List[object]
$failure=$null
try {
    $fixture=Join-Path $workspace.Path 'child process.ps1'
    @'
param([string]$Value,[int]$Code=0)
[Console]::Out.WriteLine('VALUE='+$Value)
[Console]::Error.WriteLine('ERROR-ONE')
[Console]::Error.WriteLine('ERROR-TWO')
exit $Code
'@ | Set-Content -LiteralPath $fixture -Encoding UTF8
    foreach($legacy in @($false,$true)){
        foreach($value in @('argument with spaces','path ending\')){
            $result=Invoke-PalClamTestProcess -Executable $ps -Arguments @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$fixture,'-Value',$value) -LegacyArguments:$legacy
            Check ($result.ExitCode -eq 0) 'stderr no debe convertir exito en fallo'
            Check ($result.StdOut.Contains('VALUE='+$value)) 'argumento preservado (incluidos espacios/barra final)'
            Check ($result.StdErr.Contains('ERROR-ONE') -and $result.StdErr.Contains('ERROR-TWO')) 'stderr completo, sin truncar la segunda linea'
        }
    }
    $result=Invoke-PalClamTestProcess $ps @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$fixture,'-Value','failure','-Code','17')
    Check ($result.ExitCode -eq 17) 'conservar codigo de salida nativo'
    Check ($result.StdErr.Contains('ERROR-TWO')) 'conservar detalle de fallo nativo'
    $startFailed=$false
    try {Invoke-PalClamTestProcess (Join-Path $workspace.Path 'missing-program.exe') @() | Out-Null}catch{$startFailed=$true;Check ($_.Exception.Message -notmatch 'No process is associated') 'no ocultar error de inicio con Dispose/HasExited'}
    Check $startFailed 'programa inexistente sigue siendo un error'
    $retry=New-PalClamTestWorkspace -Kind Support;$extra.Add($retry)
    $state=@{Count=0}
    $delete={param($path) $state.Count++;if($state.Count -lt 3){throw 'SIMULATED LOCK'};Remove-Item -LiteralPath $path -Recurse -Force}.GetNewClosure()
    $cleanup=Remove-PalClamTestWorkspace $retry -Attempts 4 -DelayMilliseconds 1 -DeleteAction $delete
    Check ($cleanup.Removed -and $cleanup.Attempts -eq 3) 'reintentar un bloqueo transitorio'
    $blocked=New-PalClamTestWorkspace -Kind Support;$extra.Add($blocked)
    $cleanup=Remove-PalClamTestWorkspace $blocked -Attempts 2 -DelayMilliseconds 1 -DeleteAction {param($path) throw 'SIMULATED PERMANENT LOCK'}
    Check (-not $cleanup.Removed -and $cleanup.Attempts -eq 2 -and (Test-Path -LiteralPath $blocked.Path)) 'conservar carpeta tras bloqueo persistente'
    $messages=@(Complete-PalClamTestRun -Suite 'simulacion' -Checks 1 -Failure $null -Cleanup $cleanup 6>&1)
    Check ($messages[-1] -eq 0) 'limpieza pendiente no convierte pruebas correctas en fallo'
    $messages=@(Complete-PalClamTestRun -Suite 'simulacion' -Checks 1 -Failure 'ORIGINAL FAILURE SENTINEL' -Cleanup $cleanup 6>&1)
    Check ($messages[-1] -eq 1 -and ($messages -join ' ').Contains('ORIGINAL FAILURE SENTINEL')) 'un fallo real permanece visible aunque falle limpieza'
    $unowned=Join-Path $workspace.Path 'user folder';New-Item -ItemType Directory -Path $unowned | Out-Null
    $denied=Remove-PalClamTestWorkspace ([pscustomobject]@{Path=$unowned;Token='invalid'})
    Check (-not $denied.Removed -and (Test-Path -LiteralPath $unowned)) 'no borrar carpetas ajenas'
    $lockTest=New-PalClamTestWorkspace -Kind Support;$extra.Add($lockTest)
    $file=Join-Path $lockTest.Path 'locked.txt';[IO.File]::WriteAllText($file,'TEST ONLY')
    $stream=[IO.File]::Open($file,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
    try {
        $lockedResult=Remove-PalClamTestWorkspace $lockTest -Attempts 2 -DelayMilliseconds 1
        if([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT){
            Check (-not $lockedResult.Removed) 'bloqueo real de archivo Windows detectado'
            Check (Test-Path -LiteralPath (Join-Path $lockTest.Path '.palclam-test-owned')) 'marcador de propiedad retenido mientras exista bloqueo'
        }
    } finally {$stream.Dispose()}
    $afterClose=Remove-PalClamTestWorkspace $lockTest -DelayMilliseconds 1
    Check $afterClose.Removed 'limpieza posible tras cerrar el archivo'
    Write-Host ('[OK] '+$count+' comprobaciones de procesos, stderr, bloqueos y conservacion del error.')
} catch {$failure=$_}
finally {
    foreach($item in $extra){$result=Remove-PalClamTestWorkspace $item -DelayMilliseconds 1;if(-not $result.Removed){Write-Host ('[AVISO TEST] '+$result.Path)}}
    $finalCleanup=Remove-PalClamTestWorkspace $workspace -DelayMilliseconds 1
}
if($failure){Write-Host ('[ERROR SOPORTE TEST] '+$failure.Exception.ToString());exit 1}
if(-not $finalCleanup.Removed){Write-Host ('[AVISO TEST] '+$finalCleanup.Path)}
exit 0
