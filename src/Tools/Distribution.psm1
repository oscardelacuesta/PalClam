# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ReleaseVersion = '1.2.2'
function Get-PalClamVersion { return $script:ReleaseVersion }
function Get-ReleaseRids { return @('win-x64','win-x86') }
function Get-EngineMetadata([ValidateSet('win-x64','win-x86')][string]$RuntimeId) {
    $suffix = if ($RuntimeId -eq 'win-x86') { 'win32' } else { 'x64' }
    $hash = if ($RuntimeId -eq 'win-x86') { '3b54e5f6b0139f65bffe732fed052d38b351374cb86da395c1cf505494154f89' } else { '0d9e0228b2674137ea1a2853566c98a0278ad52ab2582c3d6dbd75373848c395' }
    $asset = 'clamav-1.5.4.win.' + $suffix + '.zip'
    return [pscustomobject]@{ Version='1.5.4'; Asset=$asset; Sha256=$hash; Url=('https://github.com/Cisco-Talos/clamav/releases/download/clamav-1.5.4/' + $asset); Root=('clamav-1.5.4.win.'+$suffix) }
}
function Get-PeMachine([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = New-Object IO.BinaryReader($stream)
    try {
        if ($stream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5a4d) { throw "No es un PE: $Path" }
        $stream.Position=0x3c; $offset=$reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $stream.Length-6) { throw "Cabecera PE invalida: $Path" }
        $stream.Position=$offset
        if ($reader.ReadUInt32() -ne 0x4550) { throw "Firma PE invalida: $Path" }
        return $reader.ReadUInt16()
    } finally { $reader.Dispose(); $stream.Dispose() }
}
function Assert-PeArchitecture([string]$Path,[ValidateSet('win-x64','win-x86')][string]$RuntimeId) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Falta $Path" }
    $expected = if ($RuntimeId -eq 'win-x86') { 0x14c } else { 0x8664 }
    if ((Get-PeMachine $Path) -ne $expected) { throw "Arquitectura incorrecta ($RuntimeId): $Path" }
}
function Invoke-Checked([string]$Executable,[string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Fallo de $Executable (codigo $LASTEXITCODE). Se detiene la entrega." }
}
function Remove-BuildFolder([string]$Root,[string]$Relative) {
    $fullRoot=[IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $path=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
    if (-not $path.StartsWith($fullRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Destino de compilacion fuera del proyecto.' }
    if (Test-Path -LiteralPath $path) {
        if ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'No se borra un enlace de compilacion.' }
        for($attempt=1;$attempt -le 4;$attempt++){
            try {Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop;return}
            catch {
                if($attempt -eq 4){throw}
                [GC]::Collect();[GC]::WaitForPendingFinalizers()
                Start-Sleep -Milliseconds 150
            }
        }
    }
}
function Get-VerifiedFile([string]$Url,[string]$Path,[string]$Sha256) {
    if (Test-Path -LiteralPath $Path) {
        if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Sha256) { throw "SHA256 incorrecto en cache: $Path. No se reutiliza." }
        return
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    $partial=$Path+'.partial'
    try {
        [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $Url -OutFile $partial -UseBasicParsing -TimeoutSec 1800
        if ((Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Sha256) { throw "SHA256 incorrecto: $Path" }
        Move-Item -LiteralPath $partial -Destination $Path -Force
    } finally { if (Test-Path -LiteralPath $partial) { Remove-Item -LiteralPath $partial -Force } }
}
function Remove-DeprecatedStartAlias([string]$Root) {
    $path=Join-Path $Root 'EMPEZAR-AQUI.bat'
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $item=Get-Item -LiteralPath $path -Force
        if (-not ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -eq '735c190987decf0e3b3622ee98815795a02d2a10d47b227dfde44a1f7620a9b1') {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
Export-ModuleMember -Function *
