# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
param([string]$ProjectRoot)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
# PE, firmas y runtime sinteticos SOLO para probar el empaquetado; se eliminan al terminar.
$ErrorActionPreference='Stop'
Import-Module (Join-Path $palClamToolsDirectory 'Distribution.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $palClamToolsDirectory 'TestSupport.psm1') -Force -DisableNameChecking
# .NET Framework / Windows PowerShell 5.1 does not load both assemblies implicitly.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$workspace=New-PalClamTestWorkspace -Kind Pkg
$work=$workspace.Path
$testFailure=$null
$cleanup=$null
$logPath=Join-Path $ProjectRoot ('dist/validation/packaging-'+$workspace.Token+'.log')
$script:stage='preparacion'
$ps=Join-Path $PSHOME 'powershell.exe'
if (-not (Test-Path -LiteralPath $ps)) { $ps=Join-Path $PSHOME 'pwsh';if (-not (Test-Path -LiteralPath $ps)) { $ps=Join-Path $PSHOME 'pwsh.exe' } }
$script:count=0
function Check([bool]$Value,[string]$Label) { if (-not $Value) { throw "Fallo: $Label" };$script:count++ }
function Run([string]$Name,[string[]]$Extra,[bool]$Success=$true) {
    $script:stage=$Name
    $result=Invoke-PalClamTestProcess -Executable $ps -Arguments (@('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',(Join-Path $palClamToolsDirectory $Name),'-ProjectRoot',$work)+$Extra)
    $code=$result.ExitCode
    Add-PalClamTestLog $logPath ('=== '+$Name+' EXIT '+$code+' ==='+[Environment]::NewLine+$result.StdOut+[Environment]::NewLine+$result.StdErr+[Environment]::NewLine)
    if ($Success -and $code -ne 0) { throw ($Name+' (salida '+$code+'):'+ [Environment]::NewLine+$result.Output) }
    Check (($code -eq 0) -eq $Success) ($Name+' salida '+$code)
}
function Write-Test([string]$Relative,[string]$Value) {
    $path=Join-Path $work $Relative;New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    [IO.File]::WriteAllText($path,$Value)
}
function Pe([string]$Relative,[int]$Machine) {
    $path=Join-Path $work $Relative;New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    $bytes=New-Object byte[] 256;$bytes[0]=0x4d;$bytes[1]=0x5a
    [BitConverter]::GetBytes([int]128).CopyTo($bytes,60);[BitConverter]::GetBytes([int]0x4550).CopyTo($bytes,128)
    [BitConverter]::GetBytes([uint16]$Machine).CopyTo($bytes,132);[IO.File]::WriteAllBytes($path,$bytes)
}
try {
    # Audit each standalone consumer: a clean Windows PowerShell 5.1 child needs both assemblies.
    foreach ($name in @('DistributionSource.ps1','Portable.ps1','ValidarDistribucion.ps1','DistributionPackagingTests.ps1')) {
        $text=[IO.File]::ReadAllText((Join-Path $palClamToolsDirectory $name))
        $core=[regex]::Match($text,'(?m)^[ \t]*Add-Type -AssemblyName System\.IO\.Compression[ \t]*\r?$')
        $extensions=$text.IndexOf('Add-Type -AssemblyName System.IO.Compression.FileSystem')
        Check ($core.Success -and $extensions -gt $core.Index) ($name+' carga ambos ensamblados de compresion en orden')
    }
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt','COPYRIGHT.md','THIRD-PARTY-NOTICES.md','ThirdParty/CHECKSUMS.txt','ThirdParty/README.md','Tools/licenses-lock.json','Tools/ConstruirDistribucion.ps1','Tools/Distribution.psm1','Tools/TestSupport.psm1','Tools/PublicacionBloqueada.ps1','BUILD_FROM_SOURCE.md','Installer/PalClam.iss','Directory.Build.props','docs/TERCEROS-Y-LEGAL.md')) {
        $destination=Join-Path $work $name;New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
        Copy-Item -LiteralPath (Join-Path $ProjectRoot $name) -Destination $destination
    }
    foreach ($dir in @('ThirdParty/Licenses','ThirdParty/SourcePackages/Libraries')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $work $dir) | Out-Null
        Get-ChildItem -LiteralPath (Join-Path $ProjectRoot $dir) | Copy-Item -Destination (Join-Path $work $dir) -Recurse
    }
    Write-Test 'ThirdParty/SourcePackages/clamav-1.5.4.tar.gz' 'TEST ONLY: synthetic source payload.'
    $lock=Get-Content -LiteralPath (Join-Path $work 'Tools/licenses-lock.json') -Encoding UTF8 -Raw | ConvertFrom-Json
    $libraries=@{};$target=@{}
    foreach ($package in $lock.packages) { $key=$package.id+'/'+$package.version;$libraries[$key]=@{type='package'};$target[$key]=@{type='package';runtime=@{'lib/test.dll'=@{}}} }
    $cache=Join-Path $work 'nuget-fixture';$downloads=@()
    foreach ($id in @('Microsoft.NETCore.App.Runtime.win-x86','Microsoft.WindowsDesktop.App.Runtime.win-x86')) {
        foreach ($rid in @('win-x86','win-x64')) {
            $relative='nuget-fixture/'+($id.ToLowerInvariant() -replace 'win-x86',$rid)+'/10.0.15/LICENSE.TXT'
            Write-Test $relative 'TEST ONLY: synthetic runtime notice.'
        }
        $downloads+=@(@{name=$id;version='[10.0.15, 10.0.15]'})
    }
    foreach ($project in @('PalClam','PalClam.Service')) {
        $libs=if ($project -eq 'PalClam') { $libraries } else { @{} };$targets=if ($project -eq 'PalClam') { $target } else { @{} }
        $assets=@{libraries=$libs;targets=@{'net10.0-windows'=$targets};packageFolders=@{$cache=@{}};project=@{frameworks=@{'net10.0-windows'=@{downloadDependencies=$downloads}}}}
        Write-Test ($project+'/obj/project.assets.json') ($assets | ConvertTo-Json -Depth 15)
    }
    Run 'PrepararLicencias.ps1' @();Run 'PrepararLicencias.ps1' @('-VerifyOnly')
    $licensePath=Join-Path $work 'LICENSE-GPL-3.0.txt';$oldLicense=[IO.File]::ReadAllBytes($licensePath)
    [IO.File]::WriteAllText($licensePath,'GNU GPL version 2: wrong project license')
    Run 'PrepararLicencias.ps1' @('-VerifyOnly') $false;[IO.File]::WriteAllBytes($licensePath,$oldLicense)
    $entry=$lock.packages[0];$notice=Join-Path $work ('ThirdParty/Licenses/'+$entry.id+'-'+$entry.version+'/'+$entry.legal_files[0].file)
    $saved=[IO.File]::ReadAllBytes($notice);[IO.File]::WriteAllText($notice,'altered')
    Run 'PrepararLicencias.ps1' @('-VerifyOnly') $false;[IO.File]::WriteAllBytes($notice,$saved)
    $path=Join-Path $work 'PalClam/obj/project.assets.json';$saved=[IO.File]::ReadAllBytes($path)
    $unknown=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $unknown.libraries | Add-Member -NotePropertyName 'Unreviewed.Dependency/99.0.0' -NotePropertyValue @{type='package'}
    $unknown.targets.'net10.0-windows' | Add-Member -NotePropertyName 'Unreviewed.Dependency/99.0.0' -NotePropertyValue @{type='package';runtime=@{'lib/unknown.dll'=@{}}}
    $unknown | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $path -Encoding UTF8
    Run 'PrepararLicencias.ps1' @('-VerifyOnly') $false;[IO.File]::WriteAllBytes($path,$saved)
    $entry=@($lock.packages | Where-Object { $_.PSObject.Properties.Name -contains 'corresponding_source' })[0]
    $missing=Join-Path $work $entry.corresponding_source.file;Move-Item -LiteralPath $missing -Destination ($missing+'.held')
    Run 'PrepararLicencias.ps1' @('-VerifyOnly') $false;Move-Item -LiteralPath ($missing+'.held') -Destination $missing
    Write-Test 'PalClam/Resources/update-public.pem' 'PUBLIC TEST KEY'
    Write-Test 'Tools/private-test.pem' 'PRIVATE TEST KEY'
    Write-Test 'PalClam/Data/user-data.json' 'PRIVATE USER DATA'
    Write-Test 'Tools/cache/junk.txt' 'CACHE'
    Write-Test 'EMPEZAR_AQUI.bat' 'OWNER WRAPPER - DO NOT DISTRIBUTE'
    Write-Test 'PalClam/OMIT.BAT' 'NESTED WRAPPER - DO NOT DISTRIBUTE'
    Run 'DistributionSource.ps1' @()
    $zip=[System.IO.Compression.ZipFile]::OpenRead((Join-Path $work 'dist/source/PalClam_1.2.2_Source.zip'))
    try {
        $names=@($zip.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
        Check (-not @($names | Where-Object { $_ -match '(?i)\.bat$' }).Count) 'fuentes sin ningun BAT'
        Check ($names -contains 'BUILD_FROM_SOURCE.md') 'instrucciones PowerShell completas'
        Check ($names -contains 'Tools/ConstruirDistribucion.ps1') 'orquestador sin depender de BAT'
        Check ($names -contains 'ThirdParty/SourcePackages/Libraries/OpenMcdf-3.4.0-687fae728616.zip') 'fuente MPL requerida para reconstruir incluida'
        Check ($names -contains 'COPYING') 'GPLv3 COPYING incluida en fuentes'
        Check ($names -contains 'LICENSE-GPL-3.0.txt') 'GPLv3 integra incluida en fuentes'
        Check ($names -contains 'Directory.Build.props') 'metadatos compilados incluidos en fuentes'
        Check ($names -contains 'PalClam/Resources/update-public.pem') 'clave publica incluida'
        Check (-not @($names | Where-Object { $_ -match '(private-test|user-data|cache/junk)' }).Count) 'privadas, datos y cache excluidos'
        Check (@($names | Where-Object { $_ -like 'ThirdParty/Licenses/*' }).Count -gt 11) 'avisos originales en fuentes'
    } finally { $zip.Dispose() }
    # Inject failures into a temporary COPY of the production script, never into production files.
    # The probe runs in the child before its exit/cleanup: process exit must not hide an open handle.
    $sourceScript=[IO.File]::ReadAllText((Join-Path $palClamToolsDirectory 'DistributionSource.ps1'))
    $faults=@(
        @{Name='ArchiveConstructor';Expression='New-Object System.IO.Compression.ZipArchive($stream,[System.IO.Compression.ZipArchiveMode]::Create,$false)'},
        @{Name='ManifestWriterConstructor';Expression='New-Object System.IO.StreamWriter($manifestStream,(New-Object Text.UTF8Encoding($false)))'},
        @{Name='ManifestWrite';Expression='$writer.Write(([pscustomobject]@{Version=''1.2.2'';License=''GPL-3.0-or-later'';BatWrappersIncluded=$false;Files=$sourceRecords} | ConvertTo-Json -Depth 5))'}
    )
    foreach ($fault in $faults) {
        $marker='PC_TEST_'+$fault.Name
        Check ($sourceScript.Split(@($fault.Expression),[StringSplitOptions]::None).Length -eq 2) ('punto de fallo unico '+$fault.Name)
        $faultScript=$sourceScript.Replace($fault.Expression,('$(throw '''+$marker+''')'))
        $probe=@'
    if ($stream -and $stream.CanWrite) { Write-Host '[TEST HANDLE LEAK]' }
    else { Write-Host '[TEST HANDLE CLOSED]' }
'@
        $cleanupAnchor='    if (Test-Path -LiteralPath $partial) {'
        Check ($faultScript.Split(@($cleanupAnchor),[StringSplitOptions]::None).Length -eq 2) 'sonda antes de limpiar el parcial'
        $faultScript=$faultScript.Replace($cleanupAnchor,($probe+[Environment]::NewLine+$cleanupAnchor))
        $faultPath=Join-Path $work ('faults/'+$fault.Name+'.ps1')
        Write-Test ('faults/'+$fault.Name+'.ps1') $faultScript
        $destination=Join-Path $work ('dist/source/Failure-'+$fault.Name+'.zip')
        [IO.File]::WriteAllText($destination,'PREVIOUS OUTPUT MUST SURVIVE')
        $result=Invoke-PalClamTestProcess -Executable $ps -Arguments @('-NoLogo','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$faultPath,'-ProjectRoot',$work,'-Destination',$destination)
        Add-PalClamTestLog $logPath ('=== '+$fault.Name+' EXIT '+$result.ExitCode+' ==='+[Environment]::NewLine+$result.Output+[Environment]::NewLine)
        Check ($result.ExitCode -ne 0 -and $result.Output.Contains($marker)) ('fallo original conservado '+$fault.Name)
        Check ($result.Output.Contains('[TEST HANDLE CLOSED]') -and -not $result.Output.Contains('[TEST HANDLE LEAK]')) ('handle cerrado antes de salir '+$fault.Name)
        Check (-not (Test-Path -LiteralPath ($destination+'.partial'))) ('parcial eliminado '+$fault.Name)
        Check (-not $result.Output.Contains('[AVISO FUENTES]')) ('sin aviso de bloqueo '+$fault.Name)
        Check ([IO.File]::ReadAllText($destination) -eq 'PREVIOUS OUTPUT MUST SURVIVE') ('salida previa conservada '+$fault.Name)
    }
    foreach ($rid in @('win-x64','win-x86')) {
        $machine=if ($rid -eq 'win-x86') { 0x14c } else { 0x8664 }
        Pe ('PalClam/dist/'+$rid+'/PalClam.exe') $machine
        foreach ($name in @('clamscan.exe','freshclam.exe','sigtool.exe')) { Pe ('ThirdParty/ClamAV/'+$rid+'/'+$name) $machine }
        Write-Test ('PalClam/dist/'+$rid+'/runtime-test.dll') 'TEST ONLY runtime payload'
        Write-Test ('PalClam/dist/'+$rid+'/Data/personal.json') 'DO NOT DISTRIBUTE'
    }
    foreach ($family in @('main','daily','bytecode')) { Write-Test ('ThirdParty/OfflineSignatures/'+$family+'.cvd') ('TEST ONLY fake signature '+('x'*2048)) }
    Run 'PrepararLicencias.ps1' @()
    foreach ($rid in @('win-x64','win-x86')) {
        Run 'Portable.ps1' @('-RuntimeId',$rid)
        $zip=[System.IO.Compression.ZipFile]::OpenRead((Join-Path $work ('dist/portable/PalClam_1.2.2_Portable_'+$rid+'.zip')))
        try {
            $names=@($zip.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
            foreach ($name in @('PalClam.exe','runtime-test.dll','palclam.portable','COPYING','LICENSE-GPL-3.0.txt','Licenses/DEPENDENCIES.json','Source/PalClam_1.2.2_Source.zip','Source/clamav-1.5.4.tar.gz','Source/THIRD-PARTY-NOTICES.md','Source/docs/TERCEROS-Y-LEGAL.md')) { Check ($names -contains $name) ($rid+' '+$name) }
            foreach ($name in @('COPYING','LICENSE-GPL-3.0.txt')) {
                $stream=$zip.GetEntry($name).Open();$hasher=[Security.Cryptography.SHA256]::Create()
                try { $actual=([BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-','').ToLowerInvariant() } finally { $stream.Dispose();$hasher.Dispose() }
                Check ($actual -eq $lock.project_license.sha256) ($rid+' GPLv3 sin alterar '+$name)
            }
            Check (-not @($names | Where-Object { $_ -like 'Data/*' -and $_ -notmatch '/$' }).Count) ($rid+' sin datos personales')
            Check (@($names | Where-Object { $_ -like 'Source/Libraries/*.zip' }).Count -eq 2) ($rid+' fuentes MPL/GPL')
            Check (@($names | Where-Object { $_ -like 'OfflineSignatures/*.cvd' }).Count -eq 3) ($rid+' familias de firmas')
        } finally { $zip.Dispose() }
    }
    Pe 'ThirdParty/ClamAV/win-x86/clamscan.exe' 0x8664
    Run 'Portable.ps1' @('-RuntimeId','win-x86') $false
} catch { $testFailure=$_ }
finally {
    $cleanup=Remove-PalClamTestWorkspace -Workspace $workspace
    Add-PalClamTestLog $logPath ('Limpieza: '+$cleanup.Removed+'; etapa: '+$script:stage+'; '+$cleanup.Error+[Environment]::NewLine)
}
exit (Complete-PalClamTestRun -Suite 'empaquetado y licencias' -Checks $script:count -Failure $testFailure -Cleanup $cleanup -LogPath $logPath)
