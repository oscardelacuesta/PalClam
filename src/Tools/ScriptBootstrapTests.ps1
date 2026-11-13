# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot)
$palClamToolsDirectory=[IO.Path]::GetDirectoryName($PSCommandPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot=[IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..')) }
$ErrorActionPreference='Stop'
Import-Module (Join-Path $palClamToolsDirectory 'Distribution.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $palClamToolsDirectory 'TestSupport.psm1') -Force -DisableNameChecking
$ps=Join-Path $PSHOME 'powershell.exe'
if (-not (Test-Path -LiteralPath $ps)) { $ps=Join-Path $PSHOME 'pwsh';if(-not(Test-Path -LiteralPath $ps)){$ps=Join-Path $PSHOME 'pwsh.exe'} }
$count=0
function Check([bool]$Value,[string]$Label) { if(-not $Value){throw $Label};$script:count++ }
$workspace=New-PalClamTestWorkspace -Kind Boot
$work=$workspace.Path
$testFailure=$null
$cleanup=$null
try {
    foreach($file in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Tools') -Filter '*.ps1' -File){
        $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
        Check (-not $errors) ('sintaxis '+$file.Name)
        if ($ast.ParamBlock) {
            $bad=@($ast.ParamBlock.FindAll({param($node) $node -is [Management.Automation.Language.VariableExpressionAst] -and $node.VariablePath.UserPath -in @('PSScriptRoot','PSCommandPath','MyInvocation')},$true))
            Check ($bad.Count -eq 0) ('variable de ruta en parametro por defecto: '+$file.Name)
        }
    }
    # Model the Windows PowerShell bug: reflection variables unavailable during binding.
    $variables=New-Object 'Collections.Generic.List[Management.Automation.PSVariable]'
    $variables.Add((New-Object Management.Automation.PSVariable('PSScriptRoot','')))
    $old=[scriptblock]::Create('[CmdletBinding()] param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))')
    $failed=$false
    try { $old.InvokeWithContext($null,$variables,@()) | Out-Null } catch { $failed=$true }
    Check $failed 'el patron original debe fallar con la ruta vacia durante binding'
    $tokens=$null;$errors=$null;$controller=Join-Path $palClamToolsDirectory 'ConstruirDistribucion.ps1'
    $ast=[Management.Automation.Language.Parser]::ParseFile($controller,[ref]$tokens,[ref]$errors)
    $binding=[scriptblock]::Create($ast.ParamBlock.Extent.Text+"`n'BOUND'")
    $bound=$binding.InvokeWithContext($null,$variables,@())
    Check (@($bound) -contains 'BOUND') 'los parametros corregidos deben enlazarse con variables de ruta vacias'
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    $project=Join-Path $work 'Proyecto con espacios'
    New-Item -ItemType Directory -Force -Path (Join-Path $project 'Tools'),(Join-Path $project 'PalClam') | Out-Null
    foreach($name in @('ConstruirDistribucion.ps1','Distribution.psm1','DistributionSource.ps1')){Copy-Item -LiteralPath (Join-Path $palClamToolsDirectory $name) -Destination (Join-Path $project 'Tools')}
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'PalClam\PalClam.csproj') -Destination (Join-Path $project 'PalClam')
    Push-Location ([IO.Path]::GetTempPath())
    try {
        $copy=Join-Path $project 'Tools\ConstruirDistribucion.ps1'
        $result=Invoke-PalClamTestProcess $ps @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$copy,'-Mode','CheckPaths')
        $output=$result.Output
        Check ($result.ExitCode -eq 0 -and ($output -join "`n").Contains($project)) 'raiz por defecto, carpeta con espacios y cwd ajeno'
        $result=Invoke-PalClamTestProcess $ps @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$copy,'-Mode','CheckPaths','-ProjectRoot',$project)
        $output=$result.Output
        Check ($result.ExitCode -eq 0 -and ($output -join "`n").Contains($project)) 'raiz explicita con espacios'
        $result=Invoke-PalClamTestProcess $ps @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$copy,'-Mode','CheckPaths','-ProjectRoot',$work)
        $output=$result.Output
        Check ($result.ExitCode -ne 0) 'una raiz incorrecta no devuelve exito'
    } finally { Pop-Location }
    $alias=Join-Path $project 'EMPEZAR-AQUI.bat'
    # Copy the exact historical bytes from a fixture supplied by the test controller if available.
    [IO.File]::WriteAllText($alias,'CUSTOM FILE - MUST NOT DELETE')
    Remove-DeprecatedStartAlias $project
    Check (Test-Path -LiteralPath $alias) 'no eliminar un alias modificado por el usuario'
    $entry=Join-Path $ProjectRoot 'EMPEZAR_AQUI.bat'
    if (Test-Path -LiteralPath $entry) {
        Check (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot 'EMPEZAR-AQUI.bat'))) 'un unico EMPEZAR en el paquete de desarrollo'
        Copy-Item -LiteralPath $entry -Destination $alias -Force
        Remove-DeprecatedStartAlias $project
        Check (-not (Test-Path -LiteralPath $alias)) 'retirar automaticamente solo el alias original conocido'
        $wrapper=Get-Content -LiteralPath (Join-Path $ProjectRoot 'CREAR_INSTALADOR.bat') -Raw
        Check ($wrapper.Contains('-ProjectRoot "%~dp0."')) 'el BAT pasa una raiz explicita sin barra final ambigua'
    }
} catch { $testFailure=$_ }
finally { $cleanup=Remove-PalClamTestWorkspace -Workspace $workspace }
exit (Complete-PalClamTestRun -Suite 'arranque, parametros y rutas' -Checks $count -Failure $testFailure -Cleanup $cleanup)
