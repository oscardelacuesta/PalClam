# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

function ConvertTo-PalClamProcessArgument([AllowEmptyString()][string]$Value) {
    # Windows/.NET Framework fallback: quote data, never pass it to a command shell.
    $escaped=[regex]::Replace($Value,'(\\*)"','$1$1\"')
    $escaped=[regex]::Replace($escaped,'(\\+)$','$1$1')
    return '"'+$escaped+'"'
}
function Invoke-PalClamTestProcess {
    param([string]$Executable,[string[]]$Arguments,[switch]$LegacyArguments)
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$Executable
    $info.UseShellExecute=$false
    $info.CreateNoWindow=$true
    $location=Get-Location
    if($location.Provider.Name -eq 'FileSystem'){$info.WorkingDirectory=$location.ProviderPath}
    $info.RedirectStandardOutput=$true
    $info.RedirectStandardError=$true
    if (-not $LegacyArguments -and $info.PSObject.Properties.Name -contains 'ArgumentList') {
        foreach($argument in $Arguments){[void]$info.ArgumentList.Add($argument)}
    } else {
        $info.Arguments=(@($Arguments | ForEach-Object {ConvertTo-PalClamProcessArgument $_}) -join ' ')
    }
    $process=New-Object Diagnostics.Process
    $process.StartInfo=$info
    $started=$false
    try {
        $started=$process.Start()
        if(-not $started){throw "No se pudo iniciar $Executable"}
        # Drain both pipes concurrently; stderr is data, not a PowerShell error stream.
        $outTask=$process.StandardOutput.ReadToEndAsync()
        $errTask=$process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout=$outTask.GetAwaiter().GetResult()
        $stderr=$errTask.GetAwaiter().GetResult()
        $code=$process.ExitCode
        return [pscustomobject]@{ExitCode=$code;StdOut=$stdout;StdErr=$stderr;Output=($stdout+[Environment]::NewLine+$stderr).Trim()}
    } finally {
        try {
            if($started -and -not $process.HasExited){$process.WaitForExit()}
        } finally {$process.Dispose()}
    }
}
function New-PalClamTestWorkspace {
    param([ValidateSet('Pkg','Boot','Support')][string]$Kind)
    $token=[guid]::NewGuid().ToString('N').Substring(0,16)
    $path=Join-Path ([IO.Path]::GetTempPath()) ('PC'+$Kind+'_'+$token)
    if(Test-Path -LiteralPath $path){throw 'Colision inesperada de carpeta de prueba.'}
    New-Item -ItemType Directory -Path $path -ErrorAction Stop | Out-Null
    [IO.File]::WriteAllText((Join-Path $path '.palclam-test-owned'),$token)
    return [pscustomobject]@{Path=$path;Token=$token}
}
function Remove-PalClamTestWorkspace {
    param([object]$Workspace,[ValidateRange(1,8)][int]$Attempts=4,[ValidateRange(0,1000)][int]$DelayMilliseconds=150,[scriptblock]$DeleteAction)
    $path=''
    try {
    $path=[IO.Path]::GetFullPath([string]$Workspace.Path)
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $comparison=if([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT){[StringComparison]::OrdinalIgnoreCase}else{[StringComparison]::Ordinal}
    $marker=Join-Path $path '.palclam-test-owned'
    $reason=''
    if(-not(Test-Path -LiteralPath $path)){return [pscustomobject]@{Removed=$true;Attempts=0;Path=$path;Error=''}}
    if(-not $path.StartsWith($temp,$comparison) -or [IO.Path]::GetFileName($path) -notmatch '^PC(Pkg|Boot|Support)_[a-f0-9]{16}$' -or -not(Test-Path -LiteralPath $marker -PathType Leaf)){
        return [pscustomobject]@{Removed=$false;Attempts=0;Path=$path;Error='Se rechazo borrar una carpeta sin propiedad de prueba verificada.'}
    }
    if([IO.File]::ReadAllText($marker) -cne [string]$Workspace.Token){
        return [pscustomobject]@{Removed=$false;Attempts=0;Path=$path;Error='Identificador de propiedad de prueba incorrecto.'}
    }
    for($attempt=1;$attempt -le $Attempts;$attempt++){
        try {
            if(-not(Test-Path -LiteralPath $path)){return [pscustomobject]@{Removed=$true;Attempts=$attempt;Path=$path;Error=''}}
            $pending=New-Object 'Collections.Generic.Queue[string]'
            $pending.Enqueue($path)
            while($pending.Count){
                $directory=$pending.Dequeue()
                if((Get-Item -LiteralPath $directory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Se conserva una carpeta de prueba con enlaces; no se sigue su destino.'}
                foreach($child in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop){
                    if($child.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Se conserva una carpeta de prueba con enlaces; no se sigue su destino.'}
                    if($child.PSIsContainer){$pending.Enqueue($child.FullName)}
                }
            }
            if($DeleteAction){& $DeleteAction $path | Out-Null}else{
                # Keep the ownership marker if any child is locked.
                Get-ChildItem -LiteralPath $path -Force | Where-Object {$_.Name -ne '.palclam-test-owned'} | ForEach-Object {Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop}
                Remove-Item -LiteralPath $marker -Force -ErrorAction Stop
                try {Remove-Item -LiteralPath $path -Force -ErrorAction Stop}
                catch {
                    if(Test-Path -LiteralPath $path){[IO.File]::WriteAllText($marker,[string]$Workspace.Token)}
                    throw
                }
            }
            if(Test-Path -LiteralPath $path){throw 'La carpeta temporal sigue existiendo.'}
            return [pscustomobject]@{Removed=$true;Attempts=$attempt;Path=$path;Error=''}
        } catch {
            $reason=$_.Exception.Message
            if($attempt -lt $Attempts){
                [GC]::Collect();[GC]::WaitForPendingFinalizers()
                if($DelayMilliseconds){Start-Sleep -Milliseconds $DelayMilliseconds}
            }
        }
    }
    return [pscustomobject]@{Removed=$false;Attempts=$Attempts;Path=$path;Error=$reason}
    } catch {
        return [pscustomobject]@{Removed=$false;Attempts=0;Path=$path;Error=$_.Exception.Message}
    }
}
function Add-PalClamTestLog([string]$Path,[string]$Text) {
    try {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) -ErrorAction Stop | Out-Null
        [IO.File]::AppendAllText($Path,$Text)
    } catch {Write-Host ('[AVISO TEST] No se pudo guardar el log: '+$Path+'. '+$_.Exception.Message)}
}
function Complete-PalClamTestRun {
    param([string]$Suite,[int]$Checks,[object]$Failure,[object]$Cleanup,[string]$LogPath)
    if(-not $Cleanup.Removed){
        Write-Host ('[AVISO TEST] Limpieza pendiente: '+$Cleanup.Path+'. '+$Cleanup.Error)
    }
    if($null -ne $Failure){
        $detail=if($Failure -is [Management.Automation.ErrorRecord]){$Failure.Exception.ToString()+[Environment]::NewLine+$Failure.ScriptStackTrace}else{[string]$Failure}
        Write-Host ('[ERROR '+$Suite+'] '+$detail) -ForegroundColor Red
        if($LogPath){Write-Host ('[LOG TEST] '+$LogPath)}
        return 1
    }
    Write-Host ('[OK] '+$Checks+' comprobaciones de '+$Suite+'.') -ForegroundColor Green
    return 0
}
Export-ModuleMember -Function ConvertTo-PalClamProcessArgument,Invoke-PalClamTestProcess,New-PalClamTestWorkspace,Remove-PalClamTestWorkspace,Add-PalClamTestLog,Complete-PalClamTestRun
