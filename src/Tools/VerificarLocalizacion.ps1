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
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-ResourceMap([string]$Path) {
    [xml]$document = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $map = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::Ordinal)
    foreach ($entry in @($document.root.data)) {
        $key = [string]$entry.name
        $value = [string]$entry.value
        if ([string]::IsNullOrWhiteSpace($key) -or [string]::IsNullOrWhiteSpace($value)) {
            throw "Recurso vacio en $Path"
        }
        if ($map.ContainsKey($key)) { throw "Clave duplicada: $key en $Path" }
        $map.Add($key, $value)
    }
    return ,$map
}

function Get-Parameters([string]$Text) {
    return (([regex]::Matches($Text, '(?<!\{)\{(\d+)(?:[, :][^{}]*)?\}(?!\})') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object) -join '|')
}

try {
    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $resources = Join-Path $root 'PalClam\Localization'
    $spanish = Read-ResourceMap (Join-Path $resources 'Strings.resx')
    if ($spanish.Count -lt 30) { throw 'Falta la cobertura inicial de localizacion.' }
    $languages = @('es','en','fr','de','pt','it')
    foreach ($language in $languages) {
        $name = if ($language -eq 'es') { 'Strings.resx' } else { "Strings.$language.resx" }
        $map = Read-ResourceMap (Join-Path $resources $name)
        $different = @(Compare-Object @($spanish.Keys | Sort-Object) @($map.Keys | Sort-Object))
        if ($different.Count -ne 0) { throw "Claves ausentes o sobrantes en $name" }
        foreach ($key in $spanish.Keys) {
            if ((Get-Parameters $spanish[$key]) -cne (Get-Parameters $map[$key])) {
                throw "Parametros alterados: $name / $key"
            }
            # El parser de formato de .NET detecta llaves sin cerrar y formatos invalidos.
            $parameterIds = @([regex]::Matches($map[$key], '(?<!\{)\{(\d+)(?:[, :][^{}]*)?\}(?!\})') |
                ForEach-Object { [int]$_.Groups[1].Value })
            $argumentCount = 1
            if ($parameterIds.Count -gt 0) {
                $argumentCount = 1 + [int](($parameterIds | Measure-Object -Maximum).Maximum)
            }
            [object[]]$formatValues = @('TEST') * $argumentCount
            [void][string]::Format([Globalization.CultureInfo]::GetCultureInfo($language),
                $map[$key], $formatValues)
        }
    }
    foreach ($source in @(Get-ChildItem -LiteralPath (Join-Path $root 'PalClam') -Filter '*.cs' -Recurse -File)) {
        $text = Get-Content -LiteralPath $source.FullName -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($text, '"((?:Header|Options|Help|Language)\.[A-Za-z.]+)"')) {
            if (-not $spanish.ContainsKey($match.Groups[1].Value)) {
                throw ('Clave usada sin recurso: ' + $match.Groups[1].Value)
            }
        }
    }
    foreach ($projectName in @('PalClam.ThemeTests','PalClam.LocalizationTests')) {
        $path = Join-Path $root ($projectName + '\' + $projectName + '.csproj')
        $projectText = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        if ($projectText -notmatch 'TextCatalog.cs' -or $projectText -notmatch 'Strings\*\.resx') {
            throw "Pruebas desconectadas de los recursos reales: $projectName"
        }
    }
    Write-Host ("[OK] {0} claves en 6 idiomas, sin duplicados, con cobertura y parametros equivalentes." -f $spanish.Count) -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ('[ERROR LOCALIZACION] ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
