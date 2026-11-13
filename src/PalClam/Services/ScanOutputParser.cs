// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;

namespace PalClam.Services;

internal enum ScanFileStatus { Clean, Detected, Error }

internal sealed record ScanFileEvent(string FilePath, ScanFileStatus Status, string Detail)
{
    public string DisplayStatus => Status switch
    {
        ScanFileStatus.Clean => "Sin detección",
        ScanFileStatus.Detected => "DETECCIÓN",
        _ => "Error"
    };
}

/// <summary>
/// Lee las líneas de resultado de clamscan. No interpreta los mensajes de
/// carga de firmas como archivos y no considera los errores como archivos limpios.
/// </summary>
internal static class ScanOutputParser
{
    public static ScanFileEvent? ParseFile(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return null;
        var line = rawLine.Trim();
        var separator = line.LastIndexOf(": ", StringComparison.Ordinal);
        if (separator < 1) return null;
        var path = line[..separator].Trim();
        var tail = line[(separator + 2)..].Trim();
        if (path.Length == 0 || tail.Length == 0) return null;
        if (tail.Equals("OK", StringComparison.Ordinal))
            return new(path, ScanFileStatus.Clean, "Sin detecciones");
        if (tail.EndsWith(" FOUND", StringComparison.Ordinal))
            return new(path, ScanFileStatus.Detected, tail[..^6]);
        if (tail.EndsWith(" ERROR", StringComparison.Ordinal))
            return new(path, ScanFileStatus.Error, tail[..^6]);
        return null;
    }

    public static bool TryReadCount(string line, string field, out long value)
    {
        value = 0;
        var text = line.Trim();
        if (!text.StartsWith(field + ":", StringComparison.OrdinalIgnoreCase)) return false;
        return long.TryParse(text[(field.Length + 1)..].Trim(), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out value) && value >= 0;
    }
}
