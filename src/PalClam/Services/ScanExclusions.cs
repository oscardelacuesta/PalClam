// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;

namespace PalClam.Services;

/// <summary>
/// Exclusiones opcionales. Evita expresiones libres del usuario: solo acepta
/// rutas absolutas y construye expresiones escapadas para ClamAV.
/// Una exclusión reduce la cobertura y siempre se comunica en la interfaz.
/// </summary>
internal static class ScanExclusions
{
    public static bool TryNormalize(IEnumerable<string> values, out List<string> paths, out string error)
    {
        paths = new List<string>();
        error = "";
        foreach (var raw in values)
        {
            var value = raw.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value)) continue;
            try
            {
                if (!Path.IsPathFullyQualified(value))
                    throw new ArgumentException("Debe ser una ruta absoluta.");
                if (value.Contains('*') || value.Contains('?'))
                    throw new ArgumentException("No se admiten comodines.");
                var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
                var root = Path.GetPathRoot(full);
                if (string.Equals(root, full, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("No se permite excluir una unidad o carpeta raíz.");
                if (!paths.Contains(full, StringComparer.OrdinalIgnoreCase)) paths.Add(full);
                if (paths.Count > 50) throw new ArgumentException("Máximo 50 rutas de exclusión.");
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                error = $"Ruta: {value}\n{ex.Message}";
                paths.Clear();
                return false;
            }
        }
        return true;
    }

    public static List<string> Normalize(IEnumerable<string>? values)
    {
        if (values is null) return new();
        return TryNormalize(values, out var paths, out _) ? paths : new();
    }

    public static bool IsExcluded(string path, IEnumerable<string>? excluded)
    {
        if (excluded is null) return false;
        string full;
        try { full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)); }
        catch (Exception) { return false; }
        foreach (var exclusion in excluded)
        {
            if (string.Equals(full, exclusion, StringComparison.OrdinalIgnoreCase)) return true;
            if (full.StartsWith(exclusion + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string RegexPrefixForPath(string fullPath)
    {
        var builder = new StringBuilder("^");
        foreach (var c in fullPath)
        {
            if (c is '\\' or '/') builder.Append(@"[\\/]");
            else if (".^$*+?()[]{}|".Contains(c)) builder.Append('\\').Append(c);
            else builder.Append(c);
        }
        return builder.ToString();
    }

    public static IEnumerable<string> BuildClamArguments(IEnumerable<string> exclusions)
    {
        foreach (var path in exclusions)
        {
            var prefix = RegexPrefixForPath(path);
            // Permite excluir tanto ficheros exactos como directorios.
            // ClamAV usa sus propias expresiones regulares en ambos argumentos.
            yield return "--exclude=" + prefix + "$";
            yield return "--exclude-dir=" + prefix + @"([\\/]|$)";
        }
    }
}
